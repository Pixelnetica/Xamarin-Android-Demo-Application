using System;
using Java.Lang;
using Utils;
using ImageSdkWrapper;

namespace App
{
    class ImageSdkThread : SequentialThread
    {
        public enum Task
        {
            Callback = SequentialThread.TaskType.Quit + 1,            
        }

        public interface IThreadCallback
        {
            Action ThreadRun(ImageProcessing sdk);
        }

        /// <summary>
        /// Image SDK instance must be created and accessed from same thread
        /// </summary>
        ImageProcessing sdk;

        public ImageSdkThread() : base("ImageSdkThread")
        {
            Start();
        }

        protected override void OnThreadStarted()
        {
            sdk = new ImageSdkLibrary().NewProcessingInstance();
        }

        protected override void OnThreadComplete()
        {
            sdk.Destroy();
        }

        protected override Action OnThreadTask(int type, Java.Lang.Object args)
        {
            if (type == (int) Task.Callback)
            {
                IThreadCallback callback = (IThreadCallback)args;
                return callback.ThreadRun(sdk);

            }

            return null;
        }
    }
}