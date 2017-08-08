using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using ImageSdkWrapper;
using System;

namespace App.Main
{
    class LoadImageTask : AsyncTask<Android.Net.Uri, Java.Lang.Void, MetaImage>
    {
        readonly ContentResolver cr;
        readonly Action<MetaImage, string> callback;
        string errorText;
        internal LoadImageTask(ContentResolver cr, Action<MetaImage, string> callback)
        {
            this.cr = cr;
            this.callback = callback;
        }
        protected override MetaImage RunInBackground(params Android.Net.Uri[] @params)
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
                    errorText = string.Format("Cannot open image file {0}", imageUri);
                    Log.Error(AppLog.TAG, errorText);
                    return null;
                }

                // Scale image to supported size
                using (ImageProcessing sdk = new ImageSdkLibrary().NewProcessingInstance())
                {
                    Point sourceSize = new Point(sourceBitmap.Width, sourceBitmap.Height);
                    Point supportSize = sdk.SupportImageSize(sourceSize);

                    Bitmap scaledBitmap;
                    if (sourceSize.Equals(supportSize))
                    {
                        // Do not scale
                        scaledBitmap = sourceBitmap;
                    }
                    else
                    {
                        scaledBitmap = Bitmap.CreateScaledBitmap(sourceBitmap, supportSize.X, supportSize.Y, true);
                    }

                    // Rotate to origin
                    MetaImage sourceImage = new MetaImage(scaledBitmap, cr, imageUri);
                    MetaImage originImage = sdk.ImageWithoutRotation(sourceImage);

                    // Free source image
                    MetaImage.SafeRecycleBitmap(sourceImage, originImage);
                    GC.Collect();
                    return originImage;
                }
            }
            catch (Java.IO.FileNotFoundException e)
            {
                errorText = string.Format("Cannot locate image file {0}", imageUri);
                Log.Error(AppLog.TAG, errorText, e);
            }
            catch (Java.Lang.OutOfMemoryError e)
            {
                errorText = e.Message;
                Log.Error(AppLog.TAG, errorText, e);
            }
            catch (Java.Lang.Error e)
            {
                errorText = e.Message;
                Log.Error(AppLog.TAG, errorText, e);
            }
            return null;
            
        }

        protected override void OnPostExecute(MetaImage result)
        {
            callback(result, errorText);
        }
    }
}