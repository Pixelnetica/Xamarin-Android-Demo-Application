using System;
using Android.Views;

namespace App.Utils
{
    public class GlobalLayutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly View observable;
        private readonly Action callback;
        private readonly bool oneTime;
        private GlobalLayutListener(View observable, bool oneTime, Action callback)
        {
            this.observable = observable;
            this.callback = callback;
            this.oneTime = oneTime;
        }

        public void OnGlobalLayout()
        {
            callback();
            if (oneTime)
            {
                observable.ViewTreeObserver.RemoveOnGlobalLayoutListener(this);
            }
        }

        public static void Install(View observable, bool oneTime, Action callback)
        {
            observable.ViewTreeObserver.AddOnGlobalLayoutListener(new GlobalLayutListener(observable, oneTime, callback));
        }
    }
}