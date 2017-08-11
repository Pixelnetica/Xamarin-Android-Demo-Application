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
            Target,         // Display processing result
        };


        ImageState imageMode = ImageState.InitNothing;

        Android.Net.Uri sourceImageUri;
        MetaImage sourceImage;  // Store source to 

        Corners initCorners;    // Corners detected by SDK
        Corners userCorners;    // Corners modified by user

        Processing processing;
        MetaImage targetImage;
        bool inCropTask;

        public MainRecord(Context context)
        {
            this.context = context.ApplicationContext;
            ImageSdkLibrary.Load((Application)this.context.ApplicationContext, null, 0);
        }

        public Bitmap DisplayBitmap
        {
            get
            {
                switch (imageMode)
                {
                    case ImageState.InitNothing:
                        return null;

                    case ImageState.Source:
                        return MetaImage.SafeGetBitmap(sourceImage);

                    case ImageState.Target:
                        return MetaImage.SafeGetBitmap(targetImage);

                    default:
                        throw new InvalidOperationException(string.Format("Illegal image mode {0}", imageMode));
                }
            }
        }

        public Corners DocumentCorners { get => userCorners; }

        public PointF [] GetDocumentFrame()
        {
            Corners corners = DocumentCorners;
            if (corners != null)
            {
                PointF[] pts = new PointF[]
                {
                    new PointF(corners.Points[0].X, corners.Points[0].Y),
                    new PointF(corners.Points[1].X, corners.Points[1].Y),
                    // IMPORTANT!
                    new PointF(corners.Points[3].X, corners.Points[3].Y),
                    new PointF(corners.Points[2].X, corners.Points[2].Y)
                };
                return pts;
            }
            else
            {
                return null;
            }
        }

        public ImageState ImageMode { get => imageMode; }
        
        // Special case to prevent spinner blinking
        public ImageState DisplayImageMode { get => inCropTask ? ImageState.Target : imageMode; }

        public Processing Processing { get => processing; }

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
            // Reset Source
            imageMode = ImageState.InitNothing;
            sourceImageUri = null;
            MetaImage.SafeRecycleBitmap(sourceImage, null);
            sourceImage = null;
            initCorners = userCorners = null;

            // Reset target
            processing = Processing.Original;
            MetaImage.SafeRecycleBitmap(targetImage, null);
            targetImage = null;

            // Reset error
            errorMessage = null;

            // Wait
            waitMode = true;

            GC.Collect();

            // Load in the thread
            LoadImageTask task = new LoadImageTask(context.ContentResolver, (LoadImageTask.Result result) =>
            {
                // Store source
                sourceImageUri = uri;
                sourceImage = result.Image;

                // Store corners
                initCorners = userCorners = result.Corners;
                
                // Display source
                imageMode = ImageState.Source;
                errorMessage = result.Error;
                waitMode = false;

                // Notify Activity
                ExecuteOnVisible(callback);
            });
            task.Execute(uri);
        }

        public void OnCropImage(Processing request, Action callback)
        {
            if (sourceImage == null)
            {
                Log.Error(AppLog.TAG, "Empty image for crop!");
                return;
            }

            processing = request;
            MetaImage.SafeRecycleBitmap(targetImage, sourceImage);
            targetImage = null;
            errorMessage = null;
            waitMode = true;

            inCropTask = true;

            CropImageTask task = new CropImageTask((CropImageTask.Job result) =>
            {
                waitMode = false;
                this.errorMessage = result.errorMessage;
                inCropTask = false;

                if (string.IsNullOrEmpty(this.errorMessage))
                {
                    // Display target
                    targetImage = result.image;
                    imageMode = ImageState.Target;
                    processing = result.processing;
                }

                ExecuteOnVisible(callback);                
            });
            task.Execute(new CropImageTask.Job(sourceImage, false, userCorners, request));
        }

        public void OnShowSource(Action callback)
        {
            waitMode = false;
            imageMode = ImageState.Source;

            MetaImage.SafeRecycleBitmap(targetImage, sourceImage);
            targetImage = null;
            errorMessage = null;
            processing = Processing.Original;

            ExecuteOnVisible(callback);
        }
    }
}