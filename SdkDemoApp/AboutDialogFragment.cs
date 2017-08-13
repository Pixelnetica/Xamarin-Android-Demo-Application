using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using App.Widget;
using Android.Content.PM;
using Java.Lang;
using Android.Text;
using Android.Support.V4.App;
using Android.Text.Method;

namespace App
{
    class AboutDialogFragment : AlertDialogFragment
    {
        protected override View OnCreateCustomView(Context context, Android.Support.V7.App.AlertDialog.Builder builder, Bundle savedInstanceState)
        {
            string versionName;
            var appInfo = context.ApplicationInfo;
            try
            {
                var pkgInfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);
                versionName = pkgInfo.VersionName;
            }
            catch (PackageManager.NameNotFoundException e)
            {
                versionName = "";
            }

            ICharSequence msg = new SpannableString(GetFormattedHtml(context, Resource.String.about_message, versionName));

            DialogTitle = context.GetText(Resource.String.about_title);
            builder.SetIcon(appInfo.Icon);
            builder.SetMessage(msg);
            builder.SetCancelable(true);
            builder.SetPositiveButton(Android.Resource.String.Ok, (object sender, DialogClickEventArgs e) => { });

            return null;       
        }

        private bool linksInstalled;
        public override void OnStart()
        {
            base.OnStart();
            // Show links
            var textView = Dialog.FindViewById<TextView>(Android.Resource.Id.Message);
            if (textView != null && !linksInstalled)
            {
                textView.MovementMethod = LinkMovementMethod.Instance;
                linksInstalled = true;
            }

        }

        public static ICharSequence GetFormattedHtml(Context context, int id, params object[] args)
        {      
            for (int i = 0; i < args.Length; ++i)
            {
                args[i] = (args[i] is string) ? TextUtils.HtmlEncode((string)args[i]) : args[i];
            }

            if (Build.VERSION.SdkInt >= Build.VERSION_CODES.N)
            {
                return Html.FromHtml(
                    string.Format(
                        Html.ToHtml(
                            new SpannableString(
                                Html.FromHtml(
                                    context.GetTextFormatted(id).ToString(), 0)), 0)), 0);
            }
            else
            {
                return Html.FromHtml(
                    string.Format(
                        Html.ToHtml(
                            new SpannableString(
                                Html.FromHtml(
                                    context.GetTextFormatted(id).ToString()))
                                   ), args));
            }
        }

    }
}