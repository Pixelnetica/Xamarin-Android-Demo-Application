using Android.App;
using Android.OS;
using Android.Support.V4.App;
using System;
using System.Collections.Generic;

namespace App.Utils
{
    class Record<A> : Binder
    {
        public interface Callback
        {
            void Run(A activity);
        }

        private static readonly Handler uiHandler = new Handler(Looper.MainLooper);

        private A visibleActivity;

        private readonly List<Callback> pendingCallbacks = new List<Callback>();

        public void ExecuteOnVisible(Callback callback)
        {
            //uiHandler.Post(delegate
            //{
                if (visibleActivity == null)
                {
                    // Store callback action if activity is not shown
                    pendingCallbacks.Add(callback);
                }
                else
                {
                    // Simple call
                    callback.Run(visibleActivity);
                }
            //});
        }

        public A VisibleActivity
        {
            get => visibleActivity;
            set
            {
                this.visibleActivity = value;

                if (this.visibleActivity != null && pendingCallbacks.Count != 0)
                {
                    foreach (Callback callback in pendingCallbacks)
                    {
                        callback.Run(visibleActivity);
                    }
                    pendingCallbacks.Clear();
                }
            }
        }

        public static Rec ReadBundle<Rec>(Bundle bundle, string key) where Rec : Record<A>
        {
            if (bundle == null || string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(string.Format("Invalid bundle (0x{0:X8}) or key (0x{1:X8})", bundle, key));
            }

            return BundleCompat.GetBinder(bundle, key) as Rec;
        }

        public void WriteBundle(Bundle bundle, string key)
        {
            if (bundle == null || string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(string.Format("Invalid bundle (0x{0:X8}) or key (0x{1:X8})", bundle, key));
            }

            BundleCompat.PutBinder(bundle, key, this);
        }
    }
}