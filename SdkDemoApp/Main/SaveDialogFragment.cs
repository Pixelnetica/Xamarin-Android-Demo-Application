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
        private bool multiPage = false;

        private RadioGroup grpWriterType;
        private CheckBox chkMultiPages;

        protected override View OnCreateCustomView(Context context, Android.Support.V7.App.AlertDialog.Builder builder, Bundle savedInstanceState)
        {
            // Setup builder
            DialogTitle = DialogContext.GetString(Resource.String.save_title);
            builder.SetIcon(Resource.Drawable.ic_save_white_24dp);
            builder.SetPositiveButton(Android.Resource.String.Ok, (object sender, DialogClickEventArgs e) =>
            {
                
            });

            // Get aruments
            writerType = Arguments.GetInt(ARG_WRITER_TYPE, writerType);
            multiPage = Arguments.GetBoolean(ARG_MULTI_PAGES, multiPage);

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

            chkMultiPages.Checked = multiPage;


            return root;
        }

        public void OnCheckedChanged(RadioGroup group, int checkedId)
        {
            chkMultiPages.Enabled = (checkedId == Resource.Id.save_imagesdk_pdf || checkedId == Resource.Id.save_imagesdk_tiff);
        }


    }
}