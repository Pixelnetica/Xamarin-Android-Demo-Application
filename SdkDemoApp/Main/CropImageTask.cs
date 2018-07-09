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
using App.Utils;

namespace App.Main
{
    class CropImageTask : AsyncTask<CropImageTask.Job, Java.Lang.Void, CropImageTask.Job>
    {
        public class Job
        {
            public readonly MetaImage image;
            public readonly bool strongShadows;
            public readonly Corners corners;
            public readonly Processing processing;
            public readonly string errorMessage;
            public readonly Profiler[] profilers;
            public Job(MetaImage image, bool strongShadows, Corners corners, Processing processing, Profiler [] profilers = null)
            {
                this.image = image;
                this.strongShadows = strongShadows;
                this.corners = corners;
                this.processing = processing;
                this.profilers = profilers;
            }
            public Job(string errorMessage, Java.Lang.Throwable e = null)
            {
                this.errorMessage = errorMessage;
                Log.Error(AppLog.TAG, this.errorMessage, e);
            }
            public bool HasError { get => !string.IsNullOrEmpty(errorMessage); }
        }

        readonly Action<Job> callback;

        public CropImageTask(Action<Job> callback)
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
                    // Crop image
                    inputJob.image.StrongShadows = inputJob.strongShadows;
                    var profilerCrop = new Profiler(/*Resource.String.profile_correct_document*/);
                    Log.Debug("ImageSDK", "Test LOG");
                    MetaImage croppedImage;
                    if (inputJob.corners != null)
                    {
                        croppedImage = sdk.CorrectDocument(inputJob.image, inputJob.corners);
                    }
                    else
                    {
                        // Corners wasn't defined
                        croppedImage = inputJob.image;
                    }
                    profilerCrop.Finish();
                    if (croppedImage == null)
                    {
                        // Something wrong
                        return new Job("Cannot crop input image");
                    }

                    // Process
                    MetaImage targetImage = null;
                    var profilerProcessing = new Profiler(/*Resource.String.profile_binarization*/);
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
                    profilerProcessing.Finish();

                    // Check processing error
                    if (targetImage == null)
                    {
                        return new Job(string.Format("Failed to perform processing {0}", inputJob.processing));
                    }

                    // Cleanup
                    MetaImage.SafeRecycleBitmap(croppedImage, targetImage);
                    GC.Collect();

                    return new Job(targetImage, inputJob.strongShadows, null, inputJob.processing,
                        new Profiler[] { profilerCrop, profilerProcessing } );
                }
            }
            catch (Java.Lang.OutOfMemoryError e)
            {
                return new Job(e.Message, e);
            }
            catch (Java.Lang.Error e)
            {
                return new Job(e.Message, e);
            }
        }

        protected override void OnPostExecute(Job result)
        {
            callback(result);
        }
    }
}