using System;
using Android.Views;

namespace App.Utils
{
    public class GlobalLayutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly ViewTreeObserver observer;
        private readonly Action callback;
        public GlobalLayutListener(ViewTreeObserver observer, Action callback)
        {
            this.observer = observer;
            this.callback = callback;
        }

        public void OnGlobalLayout()
        {
            callback();
            if (observer != null && observer.IsAlive)
            {
                observer.RemoveOnGlobalLayoutListener(this);
            }
        }
    }
}