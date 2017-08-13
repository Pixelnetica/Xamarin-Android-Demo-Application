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
using Android.Support.V7.App;
using Android.Content.PM;
using Android;
using System.Collections;
using Android.Util;
using Android.Text;
using Java.Lang;

namespace App.Utils
{
    public class BaseActivity : AppCompatActivity
    {
        protected Android.Support.V7.Widget.Toolbar toolbar;

        protected static readonly RuntimePermissions RuntimePermissions = new RuntimePermissions();

        /// <summary>
        /// Must be call after SetContentView();
        /// </summary>
        protected void SetupContentLayout()
        {
            toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(App.Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            Android.Support.V7.App.ActionBar actionBar = SupportActionBar;
            actionBar.SetDisplayShowTitleEnabled(false);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            RuntimePermissions.HandleRequestPermissionsResult(this, requestCode, permissions, grantResults);
        }

        protected void SelectImages(int requestCode, int titleId, bool allowMultiple)
        {
            string title = GetString(titleId);
            SelectImages(requestCode, title, allowMultiple);
        }

        class SelectImagesCallback : RuntimePermissions.Callback
        {
            readonly int requestCode;
            readonly string title;
            readonly bool allowMultiple;
            public SelectImagesCallback(int requestCode, string title, bool allowMultiple)
            {
                this.requestCode = requestCode;
                this.title = title;
                this.allowMultiple = allowMultiple;
            }
            public void OnRuntimePermission(Activity activity, string permission, bool granted)
            {
                if (granted)
                {
                    ((BaseActivity)activity).SelectImagesGranted(requestCode, title, allowMultiple);
                }
            }
        }

        protected void SelectImages(int requestCode, string title, bool allowMultiple)
        {
            RuntimePermissions.RunWithPermission(this, Manifest.Permission.ReadExternalStorage, Resource.String.permission_query_read_storage,
                new SelectImagesCallback(requestCode, title, allowMultiple));
        }

        private void SelectImagesGranted(int requestCode, string title, bool allowMultiple)
        {
            Intent intent = new Intent(Intent.ActionPick);
            intent.SetType("image/*");
            if (allowMultiple && Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr2)
            {
                intent.PutExtra(Intent.ExtraAllowMultiple, true);
            }

            StartActivityForResult(Intent.CreateChooser(intent, title), requestCode);
        }
    }
}