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

        // Data
        const string PREFS_STRONG_SHADOWS = "STRONG_SHADOWS";
        const string PREFS_WRITER_TYPE = "WRITER_TYPE";
        const string PREFS_MULTI_PAGES = "MULTI_PAGES";

        bool strongShadows;
        int writerType;
        bool multiPages;

        public MainRecord(Application app)
        {
            ImageSdkLibrary.Load(app, "E343-49A0-T4D2-CTD5-4JF-7I9J-3T7M-OIDOE", 1);
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

        public void OpenSourceImage(ContentResolver cr, Android.Net.Uri uri, Callback callback)
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
            LoadImageTask task = new LoadImageTask(cr, (LoadImageTask.Result result) =>
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
            task.Execute(new CropImageTask.Job(sourceImage, strongShadows, userCorners, request));
        }

        private bool IsExternalStorageWritable()
        {
            string state = Android.OS.Environment.ExternalStorageState;
            return Android.OS.Environment.MediaMounted.Equals(state);
        }

        public void OnSaveImage(string fileName, Callback callback)
        {
            // Define image
            MetaImage image;
            switch (imageMode)
            {
                case ImageState.Source:
                    image = sourceImage;
                    break;

                case ImageState.Target:
                    image = targetImage;
                    break;

                default:
                    image = null;
                    break;
            }
            if (image == null)
            {
                return;
            }

            messages.Clear();
            waitMode = true;

            new SaveImageTask((SaveImageTask.Result result) =>
            {
                waitMode = false;

                if (result.HsError)
                {
                    messages.Add(new Message(Message.TypeError, result.errorMessage));
                }
                else
                {
                    messages.Add(new Message(result.writeProfiler.Id,
                        new object[] {
                            result.writeProfiler.Total,
                            System.IO.Path.GetFileName(result.outputFilePath),  // display only file name
                            (result.outputFileSize+512)/1024    // KB
                        }));
                }

                ExecuteOnVisible(callback);
            }).Execute(new SaveImageTask.Params(image, fileName, writerType, multiPages));

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

        public void LoadPreferencies(MainActivity activity)
        {
            ISharedPreferences prefs = activity.Preferences;
            strongShadows = prefs.GetBoolean(PREFS_STRONG_SHADOWS, strongShadows);
            writerType = prefs.GetInt(PREFS_WRITER_TYPE, writerType);
            multiPages = prefs.GetBoolean(PREFS_MULTI_PAGES, multiPages);
        }

        class PreferenceWriter<T> : Callback
        {
            string name;
            T value;
            string method;

            delegate ISharedPreferencesEditor Method(string name, T value);

            public PreferenceWriter(string name, T value, string method)
            {
                this.name = name;
                this.value = value;
                this.method = method;
            }
            public void Run(MainActivity activity)
            {
                ISharedPreferencesEditor editor = activity.Preferences.Edit();
                Method caller = (Method)Delegate.CreateDelegate(typeof(Method), editor, method);
                caller(name, value);
                editor.Apply();
            }
        }

        class BoolPreferenceWriter : PreferenceWriter<bool>
        {
            public BoolPreferenceWriter(string name, bool value) : base(name, value, "PutBoolean")
            {

            }
        }

        class IntPreferenceWriter : PreferenceWriter<int>
        {
            public IntPreferenceWriter(string name, int value) : base(name, value, "PutInt")
            {

            }
        }

        public bool StrongShadow
        {
            get => strongShadows;
            set
            {
                if (value != strongShadows)
                {
                    strongShadows = value;
                    ExecuteOnVisible(new BoolPreferenceWriter(PREFS_STRONG_SHADOWS, strongShadows));
                }
            }
        }

        public int WriterType {
            get => writerType;
            set
            {
                if (value != writerType)
                {
                    writerType = value;
                    ExecuteOnVisible(new IntPreferenceWriter(PREFS_WRITER_TYPE, writerType));
                }
            }
        }

        public bool MultiPages
        {
            get => multiPages;
            set
            {
                if (value != multiPages)
                {
                    multiPages = value;
                    ExecuteOnVisible(new BoolPreferenceWriter(PREFS_MULTI_PAGES, multiPages));
                }
            }
        }
    }
}