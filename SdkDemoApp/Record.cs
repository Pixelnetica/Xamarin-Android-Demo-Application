using Android.App;
using Android.OS;
using Android.Support.V4.App;
using System;
using System.Collections.Generic;

namespace Utils
{
    class Record : Binder
    {
        private static readonly Handler uiHandler = new Handler(Looper.MainLooper);

        private Activity visibleActivity;

        private readonly List<Action> pendingCallbacks = new List<Action>();

        public void ExecuteOnVisible(Action callback)
        {
            uiHandler.Post(delegate
            {
                if (visibleActivity == null)
                {
                    // Store callback action if activity is not shown
                    pendingCallbacks.Add(callback);
                }
                else
                {
                    // Simple call
                    callback();
                }
            });
        }

        public Activity VisibleActivity
        {
            get
            {
                return visibleActivity;
            }
            set
            {
                this.visibleActivity = value;

                if (this.visibleActivity != null)
                {
                    foreach (Action callback in pendingCallbacks)
                    {
                        callback();
                    }
                    pendingCallbacks.Clear();
                }
            }
        }

        public static Rec ReadBundle<Rec>(Bundle bundle, string key) where Rec : Record
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