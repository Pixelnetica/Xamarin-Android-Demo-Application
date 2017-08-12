using System;
using Android.Content;
using Java.IO;
using Android.Graphics;
using ImageSdkWrapper;
using Android.App;
using Android.Util;
using App.Utils;
using System.Collections.Generic;

namespace App.Main
{
    class MainRecord : Record<MainActivity>
    {
        Context context;

        // Perform long task
        bool waitMode;

        // Store error message
        readonly List<Message> messages = new List<Message>();

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

        public MainRecord(Activity activity)
        {
            context = activity.ApplicationContext;
            ImageSdkLibrary.Load((Application)context, null, 0);
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

        public bool WaitMode { get => waitMode; }

        public bool HasMessages { get => messages.Count != 0; }

        public Message [] WithdrawMessages()
        {
            var output = messages.ToArray();
            messages.Clear();
            return output;
        }

        public void OpenSourceImage(Android.Net.Uri uri, Callback callback)
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

            // Reset messages
            messages.Clear();

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

                // Build message
                if (result.HasError)
                {
                    messages.Add(new Message(Message.TypeError, result.Error));
                }
                else
                {
                    // Add profilers
                    messages.Add(new Message(result.Profiler.Id, new object[] { result.Profiler.Total }));
                }

                waitMode = false;

                // Notify Activity
                ExecuteOnVisible(callback);
            });
            task.Execute(uri);
        }

        public void OnCropImage(Processing request, Callback callback)
        {
            if (sourceImage == null)
            {
                Log.Error(AppLog.TAG, "Empty image for crop!");
                return;
            }

            processing = request;
            MetaImage.SafeRecycleBitmap(targetImage, sourceImage);
            targetImage = null;
            messages.Clear();
            waitMode = true;

            inCropTask = true;

            CropImageTask task = new CropImageTask((CropImageTask.Job result) =>
            {
                waitMode = false;
                if (result.HasError)
                inCropTask = false;

                if (result.HasError)
                {
                    messages.Add(new Message(Message.TypeError, result.errorMessage));
                }
                else
                {
                    // Display target
                    targetImage = result.image;
                    imageMode = ImageState.Target;
                    processing = result.processing;
                    foreach (Profiler p in result.profilers)
                    {
                        messages.Add(new Message(p.Id, new object[] { p.Total }));
                    }
                }

                ExecuteOnVisible(callback);                
            });
            task.Execute(new CropImageTask.Job(sourceImage, false, userCorners, request));
        }

        public void OnShowSource(Callback callback)
        {
            waitMode = false;
            imageMode = ImageState.Source;

            MetaImage.SafeRecycleBitmap(targetImage, sourceImage);
            targetImage = null;
            processing = Processing.Original;
            messages.Clear();

            ExecuteOnVisible(callback);
        }
    }
}