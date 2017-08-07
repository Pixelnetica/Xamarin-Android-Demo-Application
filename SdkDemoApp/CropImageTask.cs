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
using Android.Util;

namespace App
{
    class CropImageTask : AsyncTask<CropImageTask.Job, Java.Lang.Void, CropImageTask.Job>
    {
        public enum Processing
        {
            Original,   // Do nothing
            BW,
            Gray,
            Color,
        }

        public class Job
        {
            public readonly MetaImage image;
            public readonly bool strongShadows;
            public readonly Corners corners;
            public readonly Processing processing;
            public Job(MetaImage image, bool strongShadows, Corners corners, Processing processing)
            {
                this.image = image;
                this.strongShadows = strongShadows;
                this.corners = corners;
                this.processing = processing;
            }
        }

        readonly Action<Job, string> callback;

        string errorMessage;

        public CropImageTask(Action<Job, string> callback)
        {
            this.callback = callback;
        }

        protected override Job RunInBackground(params Job[] @params)
        {
            var inputJob = @params[0];
            try
            {
                // Working with ImageSDK
                using (ImageProcessing sdk = new ImageSdkLibrary().NewProcessingInstance())
                {
                    // Detect corners if not specified
                    Corners corners = inputJob.corners;
                    if (corners == null)
                    {
                        Bundle args = new Bundle();
                        corners = sdk.DetectDocumentCorners(inputJob.image, args);
                        if (corners == null || !args.GetBoolean(ImageSdkLibrary.SdkIsSmartCrop))
                        {
                            // Not certian corners detection
                            return new Job(null, inputJob.strongShadows, corners, inputJob.processing);
                        }
                    }

                    // Crop image
                    inputJob.image.StrongShadows = inputJob.strongShadows;
                    MetaImage croppedImage = sdk.CorrectDocument(inputJob.image, corners);
                    if (croppedImage == null)
                    {
                        // Something wrong
                        errorMessage = "Cannot crop input image";
                        return null;
                    }

                    // Process
                    MetaImage targetImage = null;
                    switch (inputJob.processing)
                    {
                        case Processing.Original:
                            targetImage = sdk.ImageOriginal(croppedImage);
                            break;

                        case Processing.BW:
                            targetImage = sdk.ImageBWBinarization(croppedImage);
                            break;

                        case Processing.Gray:
                            targetImage = sdk.ImageGrayBinarization(croppedImage);
                            break;

                        case Processing.Color:
                            targetImage = sdk.ImageColorBinarization(croppedImage);
                            break;
                    }

                    // Check processing error
                    if (targetImage == null)
                    {
                        errorMessage = string.Format("Failed to perform processing {0}", inputJob.processing);
                        return null;
                    }

                    // Cleanup
                    MetaImage.SafeRecycleBitmap(croppedImage, targetImage);
                    GC.Collect();

                    return new Job(targetImage, inputJob.strongShadows, corners, inputJob.processing);
                }
            }
            catch (Java.Lang.OutOfMemoryError e)
            {
                errorMessage = e.Message;
                Log.Error(AppLog.TAG, errorMessage, e);
            }
            catch (Java.Lang.Error e)
            {
                errorMessage = e.Message;
                Log.Error(AppLog.TAG, errorMessage, e);
            }
            return null;
        }

        protected override void OnPostExecute(Job result)
        {
            callback(result, errorMessage);
        }
    }
}