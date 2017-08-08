using System;
using Android.Content;
using Java.IO;
using Android.Graphics;
using ImageSdkWrapper;
using Android.App;
using Android.Util;
using App.Utils;

namespace App.Main
{
    class MainRecord : Record
    {
        Context context;

        // Perform long task
        bool waitMode;

        // Store error message
        string errorMessage;

        public enum ImageState
        {
            InitNothing,    // No image to show
            Source,         // Display source image
            CropOrigin,     // Display source image with manual corners
            Target,         // Display processing result
        };


        ImageState imageMode = ImageState.InitNothing;

        Android.Net.Uri sourceImageUri;
        MetaImage sourceImage;  // Store source to 

        Corners initCorners;    // Corners detected by SDK
        Corners userCorners;    // Corners modified by user
        CropImageTask.Processing processing = CropImageTask.Processing.Gray;    // by default
        MetaImage targetImage;  // 

        MetaImage displayImage;   // current bitmap


        public MainRecord(Context context)
        {
            this.context = context.ApplicationContext;
            ImageSdkLibrary.Load((Application)this.context.ApplicationContext, null, 0);
        }

        public Bitmap DisplayBitmap
        {
            get
            {
                return MetaImage.SafeGetBitmap(displayImage);
            }
        }

        public ImageState ImageMode
        {
            get
            {
                return imageMode;
            }
        }

        public bool WaitMode
        {
            get
            {
                return waitMode;
            }
        }

        public bool HasError
        {
            get
            {
                return !string.IsNullOrEmpty(errorMessage);
            }
        }

        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
        }

        public void ResetError()
        {
            errorMessage = null;
        }

        public void OpenSourceImage(Android.Net.Uri uri, Action callback)
        {
            // Reset previous
            sourceImageUri = null;
            sourceImage = null;
            errorMessage = null;

            // Wait
            waitMode = true;
            
            // Load in the thread
            LoadImageTask task = new LoadImageTask(context.ContentResolver, (MetaImage image, string message) =>
            {
                // Store source
                sourceImageUri = uri;
                sourceImage = image;

                // Display source
                displayImage = sourceImage;
                imageMode = ImageState.Source;
                this.errorMessage = message;
                waitMode = false;

                // Notify Activity
                ExecuteOnVisible(callback);
            });
            task.Execute(uri);
        }

        public void OnCropImage(Action callback)
        {
            if (sourceImage == null)
            {
                Log.Error(AppLog.TAG, "Empty image for crop!");
            }

            targetImage = null;
            errorMessage = null;
            waitMode = true;

            CropImageTask task = new CropImageTask((CropImageTask.Job result, string errorMessage) =>
            {
                waitMode = false;
                this.errorMessage = errorMessage;

                if (string.IsNullOrEmpty(this.errorMessage))
                {
                    if (result.image == null)
                    {
                        // Corners not detected
                        // Go to manual crop mode
                        initCorners = result.corners;
                        userCorners = Corners.SafeClone(initCorners);
                        imageMode = ImageState.CropOrigin;
                    }
                    else
                    {
                        // Display target
                        targetImage = result.image;
                        displayImage = targetImage;
                        imageMode = ImageState.Target;
                    }
                }

                ExecuteOnVisible(callback);
                
            });
            task.Execute(new CropImageTask.Job(sourceImage, false, userCorners, processing));

        }
    }
}