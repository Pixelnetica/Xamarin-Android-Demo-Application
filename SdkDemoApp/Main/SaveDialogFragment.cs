using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using App.Widget;
using ImageSdkWrapper;

namespace App.Main
{
    public class SaveDialogFragment : AlertDialogFragment, RadioGroup.IOnCheckedChangeListener
    {
        public const string ARG_WRITER_TYPE = "WRITER_TYPE";
        public const string ARG_MULTI_PAGES = "MULTI_PAGES";

        private int writerType = -1;
        private bool multiPages = false;

        private RadioGroup grpWriterType;
        private CheckBox chkMultiPages;

        public interface Callback
        {
            void OnSaveDialogOk(int writerType, bool multiPages);
        }

        public static void Show(Android.Support.V4.App.FragmentManager fm, int writerType, bool multiPages)
        {
            var args = new Bundle();
            args.PutInt(ARG_WRITER_TYPE, writerType);
            args.PutBoolean(ARG_MULTI_PAGES, multiPages);

            SaveDialogFragment dialog = new SaveDialogFragment();
            dialog.Arguments = args;
            dialog.Show(fm, "SaveDialogFragment");
        }

        protected override View OnCreateCustomView(Context context, Android.Support.V7.App.AlertDialog.Builder builder, Bundle savedInstanceState)
        {
            // Setup builder
            DialogTitle = DialogContext.GetString(Resource.String.save_title);
            builder.SetIcon(Resource.Drawable.ic_save_white_24dp);
            builder.SetPositiveButton(Android.Resource.String.Ok, (object sender, DialogClickEventArgs e) =>
            {
                switch(grpWriterType.CheckedRadioButtonId)
                {
                    case Resource.Id.save_system_jpeg:
                        writerType = ImageSdkLibrary.ImageWriterJpeg;
                        break;

                    case Resource.Id.save_system_png:
                        writerType = ImageSdkLibrary.ImageWriterPng;
                        break;

                    case Resource.Id.save_imagesdk_png:
                        writerType = ImageSdkLibrary.ImageWriterPngExt;
                        break;

                    case Resource.Id.save_imagesdk_pdf:
                        writerType = ImageSdkLibrary.ImageWriterPdf;
                        break;

                    case Resource.Id.save_imagesdk_tiff:
                        writerType = ImageSdkLibrary.ImageWriterTiff;
                        break;
                }

                multiPages = chkMultiPages.Checked;


                Callback callback = Activity as Callback;
                callback.OnSaveDialogOk(writerType, multiPages);                
            });

            builder.SetNegativeButton(Android.Resource.String.Cancel, (object sender, DialogClickEventArgs e) =>
            {
                // Nothing
            });

            // Get aruments
            writerType = Arguments.GetInt(ARG_WRITER_TYPE, writerType);
            multiPages = Arguments.GetBoolean(ARG_MULTI_PAGES, multiPages);

            // Create view
            var inflater = LayoutInflater.From(context);
            View root = inflater.Inflate(Resource.Layout.Save, null);
            grpWriterType = root.FindViewById<RadioGroup>(Resource.Id.save_writer_type);
            grpWriterType.SetOnCheckedChangeListener(this);
            chkMultiPages = root.FindViewById<CheckBox>(Resource.Id.save_multi_pages);

            switch(writerType)
            {
                case ImageSdkLibrary.ImageWriterJpeg:
                    grpWriterType.Check(Resource.Id.save_system_jpeg);
                    break;

                case ImageSdkLibrary.ImageWriterPng:
                    grpWriterType.Check(Resource.Id.save_system_png);
                    break;

                case ImageSdkLibrary.ImageWriterPngExt:
                    grpWriterType.Check(Resource.Id.save_imagesdk_png);
                    break;

                case ImageSdkLibrary.ImageWriterPdf:
                    grpWriterType.Check(Resource.Id.save_imagesdk_pdf);
                    break;

                case ImageSdkLibrary.ImageWriterTiff:
                    grpWriterType.Check(Resource.Id.save_imagesdk_tiff);
                    break;
            }

            chkMultiPages.Checked = multiPages;


            return root;
        }

        public void OnCheckedChanged(RadioGroup group, int checkedId)
        {
            chkMultiPages.Enabled = (checkedId == Resource.Id.save_imagesdk_pdf || checkedId == Resource.Id.save_imagesdk_tiff);
        }
    }
}