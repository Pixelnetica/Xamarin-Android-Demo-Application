using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using App.Utils;
using ImageSdkWrapper;
using Java.Lang;
using System;

namespace App.Main
{
    class LoadImageTask : AsyncTask<Android.Net.Uri, Java.Lang.Void, LoadImageTask.Result>
    {
        internal class Result
        {
            public readonly MetaImage Image;
            public readonly Corners Corners;
            public readonly Profiler Profiler;
            public readonly string Error;

            public Result(MetaImage image, Corners corners, Profiler profiler)
            {
                Image = image;
                Corners = corners;
                Profiler = profiler;
            }

            public Result(string error, Throwable e = null)
            {
                this.Error = error;
                Log.Error(AppLog.TAG, error, e);
            }

            public bool HasError { get => !string.IsNullOrEmpty(Error); }
            public bool HasCorners { get => this.Corners != null; }
        }
        readonly ContentResolver cr;
        readonly Action<Result> callback;
        internal LoadImageTask(ContentResolver cr, Action<Result> callback)
        {
            this.cr = cr;
            this.callback = callback;
        }
        protected override Result RunInBackground(params Android.Net.Uri[] @params)
        {
            var imageUri = @params[0];
            try
            {
                // Open source image
                Bitmap sourceBitmap;
                using (var stream = cr.OpenInputStream(imageUri))
                {
                    sourceBitmap = BitmapFactory.DecodeStream(stream);
                }
                if (sourceBitmap == null)
                {
                    return new Result(string.Format("Cannot open image file {0}", imageUri));
                }

                // Scale image to supported size
                using (ImageProcessing sdk = new ImageSdkLibrary().NewProcessingInstance())
                {
                    Point sourceSize = new Point(sourceBitmap.Width, sourceBitmap.Height);
                    //Point supportSize = sdk.SupportImageSize(sourceSize);
                    var scaledBitmap = sourceBitmap;

                    // Rotate to origin
                    MetaImage sourceImage = new MetaImage(scaledBitmap, imageUri.ToString());
                    MetaImage originImage = sdk.ImageWithoutRotation(sourceImage);

                    // Free source image
                    MetaImage.SafeRecycleBitmap(sourceImage, originImage);

                    // Try to detect document corners
                    Bundle args = new Bundle();
                    int start = System.Environment.TickCount;
                    var profiler = new Profiler(/*Resource.String.profile_detect_corners*/);
                    bool bDocumentAreaChecked, bDocumentDistortionChecked;
                    Corners corners = sdk.DetectDocumentCorners(originImage, false, out bDocumentAreaChecked, out bDocumentDistortionChecked);

                    profiler.Finish();
                    
                    // Free memory
                    GC.Collect();

                    // OK
                    return new Result(originImage, corners, profiler);
                }
            }
            catch (Java.IO.FileNotFoundException e)
            {
                return new Result(string.Format("Cannot locate image file {0}", imageUri));
            }
            catch (Java.Lang.OutOfMemoryError e)
            {
                return new Result(e.Message, e);
            }
            catch (Java.Lang.Error e)
            {
                return new Result(e.Message, e);
            }
        }

        protected override void OnPostExecute(Result result)
        {
            callback(result);
        }
    }
}