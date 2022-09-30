using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.App;
using Java.Lang;
using Android.Text;

namespace App.Widget
{
    using AlertDialog = Android.Support.V7.App.AlertDialog;

    public abstract class AlertDialogFragment : Android.Support.V4.App.DialogFragment
    {
        private int dialogTheme = Resource.Style.Theme_AppCompat_Dialog_Alert;//Resource.Style.AppTheme_Dialog_Alert;
        private bool hasWindowTitle;
        private string dialogTitle;
        private Context dialogContext;
        private View customView;
        private bool initializeDialog;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Setup theme on creation, take chance caller to change theme
            if (savedInstanceState == null)
            {
                DefineDialogStyle();
            }

            // Check theme has window title
            Context context = new Android.Support.V7.View.ContextThemeWrapper(Activity.ApplicationContext, Theme);
            var array = context.ObtainStyledAttributes(new int[] { Android.Resource.Attribute.WindowNoTitle });
            hasWindowTitle = !array.GetBoolean(0, false);
            array.Recycle();
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            if (savedInstanceState != null)
            {
                OnLoadIstanceState(savedInstanceState);
            }

            var builder = new AlertDialog.Builder(Activity, Theme);
            dialogContext = builder.Context;
            customView = OnCreateCustomView(dialogContext, builder, savedInstanceState);
            builder.SetView(customView);

            if (hasWindowTitle)
            {
                // Remove builder's title if view already contains title
                builder.SetTitle((string)null);
            }
            else if (dialogTitle != null)
            {
                builder.SetTitle(dialogTitle);
            }

            return builder.Create();
        }

        public virtual void OnLoadIstanceState(Bundle savedInstanceState)
        {
            dialogTitle = savedInstanceState.GetString("DIALOG_TITLE", dialogTitle);
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutString("DIALOG_TITLE", dialogTitle);
        }

        protected virtual void DefineDialogStyle()
        {
            SetStyle(StyleNormal, dialogTheme);
        }
        protected abstract View OnCreateCustomView(Context context, AlertDialog.Builder builder, Bundle savedInstanceState);

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Update dialog title
            UpdateTitle();
            initializeDialog = true;
            return null;
        }

        public override void OnStart()
        {
            base.OnStart();

            if (initializeDialog)
            {
                OnInitDialog();
                initializeDialog = false;
            }
        }

        // Setup AlertDialog after creation
        protected virtual void OnInitDialog()
        {

        }

        protected void UpdateTitle()
        {
            if (Dialog != null)
            {
                // NOTE: AlertDialog.setTitle() put title both into window title bar and into
                // AlertDialog's custom title view
                if (hasWindowTitle)
                {
                    Dialog.Window.SetTitle(dialogTitle);
                    Dialog.Window.Attributes.Title = dialogTitle;
                }
                else
                {
                    Dialog.SetTitle(dialogTitle);
                }
            }
        }

        public Context DialogContext { get => dialogContext; }
        public string DialogTitle
        {
            get => dialogTitle; set
            {
                if (dialogTitle != value)
                {
                    dialogTitle = value;
                    UpdateTitle();
                }
            }
        }

        // Helper
        public static ICharSequence GetFormattedHtml(Context context, int id, params object[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                args[i] = (args[i] is string) ? TextUtils.HtmlEncode((string)args[i]) : args[i];
            }

            if (Build.VERSION.SdkInt >= (BuildVersionCodes) 24)
            {
                return Html.FromHtml(
                    string.Format(
                        Html.ToHtml(
                            new SpannableString(
                                Html.FromHtml(
                                    context.GetTextFormatted(id).ToString(), FromHtmlOptions.ModeLegacy)), ToHtmlOptions.ParagraphLinesConsecutive), args), FromHtmlOptions.ModeLegacy);
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