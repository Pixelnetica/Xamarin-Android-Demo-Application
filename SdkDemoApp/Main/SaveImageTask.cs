using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ImageSdkWrapper;
using App.Utils;

namespace App.Main
{
    using Java.Lang;
    using System.IO;
    using Message = Utils.Message;
    class SaveImageTask : AsyncTask<SaveImageTask.Params, Java.Lang.Void, SaveImageTask.Result>
    {
        // Input
        public class Params
        {
            public readonly MetaImage image;
            public readonly string filePath;
            public readonly ImageWriter.EImageFileType writerType;
            public readonly bool multiPages;

            public Params(MetaImage image, string filePath, int writerType, bool multiPages)
            {
                this.image = image;
                this.filePath = filePath;
                this.writerType = (ImageWriter.EImageFileType)writerType;
                this.multiPages = multiPages;
            }
        }

        // Output
        public class Result
        {
            public readonly string outputFilePath;
            public readonly long outputFileSize;
            public readonly string errorMessage;

            public Result(string outputFilePath, long outputFileSize, Profiler writeProfiler)
            {
                this.outputFilePath = outputFilePath;
                this.outputFileSize = outputFileSize;
            }

            public Result(string errorMessage)
            {
                this.errorMessage = errorMessage;
            }

            public bool HasError { get => !string.IsNullOrEmpty(errorMessage); }
        }

        Action<Result> callback;

        public SaveImageTask(Action<Result> callback)
        {
            this.callback = callback;
        }

        static string GetExt(ImageWriter.EImageFileType type, string defFilePath)
        {
            switch (type)
            {
                case ImageWriter.EImageFileType.Jpeg: return ".jpg";
                case ImageWriter.EImageFileType.Png:
                case ImageWriter.EImageFileType.PngExt: return ".png";
                case ImageWriter.EImageFileType.WebM: return ".webm";
                case ImageWriter.EImageFileType.PdfPng:
                case ImageWriter.EImageFileType.Pdf: return ".pdf";
                case ImageWriter.EImageFileType.Tiff: return ".tif";
                default:
                    return Path.GetExtension(defFilePath);
            }
        }

        protected override Result RunInBackground(params Params[] @params)
        {
            Params input = @params[0];

            try
            {
                string extensions = GetExt(input.writerType, Path.GetExtension(input.filePath));

                int maxPages = 1;
                var filePath = Path.ChangeExtension(input.filePath, extensions);
                using (ImageWriter writer = new ImageWriter((ImageWriter.EImageFileType)input.writerType))
                {
                    writer.Open(filePath);

                    switch ((ImageWriter.EImageFileType)input.writerType)
                    {
                        case ImageWriter.EImageFileType.Jpeg:
                            writer.Configure(ImageWriter.EConfigParam.CompressionQuality, 80);
                            //bundle.PutInt(ImageWriter.ConfigCompression, 80);
                            break;

                        case ImageWriter.EImageFileType.Png:
                            break;
                        case ImageWriter.EImageFileType.PngExt:
                            break;

                        case ImageWriter.EImageFileType.WebM:
                            break;

                        case ImageWriter.EImageFileType.PdfPng:
                        case ImageWriter.EImageFileType.Pdf:
                            maxPages = 3;
                            //writer.Configure(ImageWriter.EConfigParam.Paper, input.paperFormat);
                            writer.Configure(ImageWriter.EConfigParam.Units, ImageWriter.EUnitsConfigValues.Inches);
                            writer.Configure(ImageWriter.EConfigParam.FooterHeight, 25);
                            //writer.Configure(ImageWriter.EConfigParam.FooterText, "Test");
                            break;

                        case ImageWriter.EImageFileType.Tiff:
                            maxPages = 3;
                            break;

                        default:
                            break;
                    }

                    //writer.Configure().Configure(bundle);
                    // Simulate multipages
                    int pageCount = input.multiPages ? maxPages : 1;
                    for (int i = 0; i < pageCount; ++i)
                    {
                        if (input.writerType == ImageWriter.EImageFileType.PdfPng)
                        {
                            if (i == 0)
                            {
                                using (ImageWriter subWriter = new ImageWriter(ImageWriter.EImageFileType.PngExt))
                                {
                                    subWriter.Open(System.IO.Path.ChangeExtension(filePath, ".png"));
                                    filePath = subWriter.Write(input.image);
                                }
                            }

                            writer.WriteFile(filePath, ImageWriter.EPngPdfImageFileType.Png, input.image.ExifOrientation);
                        }
                        else
                        {
                            writer.Write(input.image);
                        }
                    }
                }
                    // Calculate file size
                    long outputFileSize = new FileInfo(filePath).Length;

                // Build result
                return new Result(filePath, outputFileSize, null);
            }
            catch (Exception e)
            {
                return new Result(e.Message);
            }
        }

        protected override void OnPostExecute(Result result)
        {
            callback(result);
        }
    }
}