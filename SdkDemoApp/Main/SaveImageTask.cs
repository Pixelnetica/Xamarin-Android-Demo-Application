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
            public readonly int writerType;
            public readonly bool multiPages;

            public Params(MetaImage image, string filePath, int writerType, bool multiPages)
            {
                this.image = image;
                this.filePath = filePath;
                this.writerType = writerType;
                this.multiPages = multiPages;
            }
        }

        // Output
        public class Result
        {
            public readonly string outputFilePath;
            public readonly long outputFileSize;
            public readonly Profiler writeProfiler;
            public readonly string errorMessage;

            public Result(string outputFilePath, long outputFileSize, Profiler writeProfiler)
            {
                this.outputFilePath = outputFilePath;
                this.outputFileSize = outputFileSize;
                this.writeProfiler = writeProfiler;
            }

            public Result(string errorMessage)
            {
                this.errorMessage = errorMessage;
            }

            public bool HsError { get => !string.IsNullOrEmpty(errorMessage); }
        }

        Action<Result> callback;

        public SaveImageTask(Action<Result> callback)
        {
            this.callback = callback;
        }


        protected override Result RunInBackground(params Params[] @params)
        {
            Params input = @params[0];

            try
            {
                // Change input file extensions and setup simple config params
                string extensions;
                int maxPages = 1;
                var bundle = new Bundle();
                switch (input.writerType)
                {
                    case ImageSdkLibrary.ImageWriterJpeg:
                        extensions = ".jpg";
                        bundle.PutInt(ImageWriter.ConfigCompression, 80);
                        break;

                    case ImageSdkLibrary.ImageWriterPng:
                    case ImageSdkLibrary.ImageWriterPngExt:
                        extensions = ".png";
                        break;

                    case ImageSdkLibrary.ImageWriterWebM:
                        extensions = ".webm";
                        break;

                    case ImageSdkLibrary.ImageWriterPdf:
                        extensions = ".pdf";
                        maxPages = 3;
                        bundle.PutInt(ImageWriter.ConfigPaper, ImageWriter.PaperA4);
                        break;

                    case ImageSdkLibrary.ImageWriterTiff:
                        extensions = ".tif";
                        maxPages = 3;
                        break;

                    default:
                        extensions = Path.GetExtension(input.filePath);
                        break;
                }
                var filePath = Path.ChangeExtension(input.filePath, extensions);

                Profiler writeProfiler;
                using (ImageWriter writer = new ImageSdkLibrary().NewImageWriterInstance(input.writerType))
                {
                    writer.Open(filePath);
                    writer.Configure(bundle);
                    // Simulate multipages
                    int pageCount = input.multiPages ? maxPages : 1;
                    writeProfiler = new Profiler(Resource.String.profile_write_file);                    
                    for (int i = 0; i < pageCount; ++i)
                    {
                        writer.Write(input.image);
                    }
                    writeProfiler.Finish();                    
                }

                // Calculate file size
                long outputFileSize = new FileInfo(filePath).Length;

                // Build result
                return new Result(filePath, outputFileSize, writeProfiler);
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