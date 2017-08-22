using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Content.PM;
using Android;
using Android.Widget;
using Android.Views;
using Android.Support.Design.Widget;
using App.Utils;
using App.Widget;
using Android.Util;
using Android.Graphics;
using static Android.Widget.AdapterView;
using System;
using System.Linq;
using Android.Preferences;

namespace App.Main
{
    using AlertDialog = Android.Support.V7.App.AlertDialog;
    using Message = App.Utils.Message;

    [Activity(Label = "@string/MainActivityTitle", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/AppTheme")]
    public class MainActivity : BaseActivity, SaveDialogFragment.Callback  
    {
        MainRecord record;
        const string BUNDLE_MAIN_RECORD = "MAIN_RECORD";

        private const int OPEN_SOURCE_IMAGE = 200;

        ImageView imageView;
        ImageFrame imageFrame;
        Button btnOpen;
        //Button btnShot;
        //Button btnEdit;
        SafeSpinner spnColor;
        Button btnSave;
        View progressHolder;

        private class UpdateCallback : MainRecord.Callback
        {
            public void Run(MainActivity activity)
            {
                activity.UpdateView();
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            SetupContentLayout();


            if (bundle == null)
            {
                record = new MainRecord(Application);
                record.LoadPreferencies(this);
            }
            else
            {
                record = MainRecord.ReadBundle<MainRecord>(bundle, BUNDLE_MAIN_RECORD);
            }

            // Setup controls
            imageView = FindViewById<ImageView>(Resource.Id.image_holder);
            imageFrame = FindViewById<ImageFrame>(Resource.Id.image_frame);
            btnOpen = FindViewById<Button>(Resource.Id.btn_open_image);
            btnOpen.Click += delegate { OpenImage(); };
            //btnShot = FindViewById<Button>(Resource.Id.btn_take_photo);
            //btnShot.Click += delegate { TakePhoto(); };
            //btnEdit = FindViewById<Button>(Resource.Id.btn_edit_image);
            //btnEdit.Click += delegate { EditImage(); };

            spnColor = FindViewById<SafeSpinner>(Resource.Id.spn_color_mode);
            /*spnColor.Adapter = new SimpleImageArrayAdapter(SupportActionBar.ThemedContext, new int[]
            {
                Resource.Drawable.ic_description_white_24dp,
                Resource.Drawable.ic_camera_singlemode,
                Resource.Drawable.ic_bw_cp,
                Resource.Drawable.ic_greyscale_cp,
                Resource.Drawable.ic_color_cp,
            });*/
            spnColor.Adapter = new ImageTextSpinnerAdapter(SupportActionBar.ThemedContext, new ImageTextSpinnerAdapter.Item[]
                {
                    new ImageTextSpinnerAdapter.Item(Resource.Drawable.ic_description_white_24dp, Resource.String.processing_source),
                    new ImageTextSpinnerAdapter.Item(Resource.Drawable.ic_camera_singlemode, Resource.String.processing_origin),
                    new ImageTextSpinnerAdapter.Item(Resource.Drawable.ic_bw_cp, Resource.String.processing_bw),
                    new ImageTextSpinnerAdapter.Item(Resource.Drawable.ic_greyscale_cp, Resource.String.processing_gray),
                    new ImageTextSpinnerAdapter.Item(Resource.Drawable.ic_color_cp, Resource.String.processing_color),
                });
            spnColor.ItemUserSelected = OnColorItemSelected;
            spnColor.RestoreStyleBackground(SupportActionBar.ThemedContext);

            //SpinnerHelper.RestoreBackground(SupportActionBar.ThemedContext, spnColor);
            //spinnerHelper.AddSpinner(spnColor, new SpinnerHelper.OnItemSelected(OnColorItemSelected));

            btnSave = FindViewById<Button>(Resource.Id.btn_save_image);
            btnSave.Click += delegate { SaveImage(); };
            progressHolder = FindViewById(Resource.Id.progress_holder);

            // Update views when layout complete
            GlobalLayutListener.Install(imageView, true, () => { UpdateView(); });
        }

        public ISharedPreferences Preferences { get => PreferenceManager.GetDefaultSharedPreferences(this); }

        static readonly Processing[] processingItems = new Processing[] { Processing.Original, Processing.BW, Processing.Gray, Processing.Color };

        private void OnColorItemSelected(Spinner spinner, int position, long id)
        {
            if (position == 0)
            {
                // Special case: reset to source
                ShowSource();
            }
            else if (position > 0 && position <= processingItems.Length)
            {
                CropImage(processingItems[position-1]);
            }
            else
            {
                Log.Error(AppLog.TAG, "Unknown processing mode " + position.ToString());
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            record.WriteBundle(outState, BUNDLE_MAIN_RECORD);
        }

        protected override void OnStart()
        {
            base.OnStart();
            record.VisibleActivity = this;
        }

        protected override void OnStop()
        {
            record.VisibleActivity = null;
            base.OnStop();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);
            return true;
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var item = menu.FindItem(Resource.Id.action_strong_shadows);
            item.SetChecked(record.StrongShadow);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch(item.ItemId)
            {
                case Resource.Id.action_about:
                    ShowAbout();
                    return true;
                case Resource.Id.action_strong_shadows:
                    record.StrongShadow = !record.StrongShadow;
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            switch (requestCode)
            {
                case OPEN_SOURCE_IMAGE:
                    if (resultCode == Result.Ok)
                    {
                        Android.Net.Uri selectedImage = data.Data;
                        OnOpenImage(selectedImage);
                    }
                    break;
            }
        }
        
        private void OnOpenImage(Android.Net.Uri imageUri)
        {
            record.OpenSourceImage(ContentResolver, imageUri, new UpdateCallback());
            UpdateView();
        }

        private void OpenImage()
        {
            SelectImages(OPEN_SOURCE_IMAGE, Resource.String.select_picture_title, false);
        }

        private void TakePhoto()
        {
            Snackbar.Make(imageView, "Sorry. Camera doesn't supported yet.", Snackbar.LengthIndefinite).SetAction(Resource.String.action_close, (View view) => { }).Show();
        }

        private void ShowSource()
        {
            record.OnShowSource(new UpdateCallback());
        }

        private void EditImage()
        {
            ShowSource();
            Snackbar.Make(imageView, "Sorry. Manual image editing doesn't supported yet.", Snackbar.LengthIndefinite).SetAction(Resource.String.action_close, (View view) => { }).Show();
        }

        private void CropImage(Processing processing)
        {
            record.OnCropImage(processing, new UpdateCallback());
            UpdateView();
        }

        private void SaveImage()
        {
            SaveDialogFragment.Show(SupportFragmentManager, record.WriterType, record.MultiPages);
        }

        class SaveImageCallback : RuntimePermissions.Callback
        {
            public void OnRuntimePermission(Activity activity, string permission, bool granted)
            {
                ((MainActivity)activity).SaveImageGranted();
            }
        }

        public void OnSaveDialogOk(int writerType, bool multiPages)
        {
            record.WriterType = writerType;
            record.MultiPages = multiPages;

            // Request write permissions
            RuntimePermissions.RunWithPermission(this, Manifest.Permission.ReadExternalStorage, Resource.String.permission_query_write_storage,
                new SaveImageCallback());
        }

        public long UnixTimeNow()
        {
            var epochTicks = new DateTime(1970, 1, 1).Ticks;
            return ((DateTime.UtcNow.Ticks - epochTicks) / TimeSpan.TicksPerSecond);
        }

        private void SaveImageGranted()
        {
            // Build file name
            var folder = new Java.IO.File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures), "Pixelnetica");
            folder.Mkdirs();

            string fileName = string.Format("SdkDemo-{0:X08}.jpg", UnixTimeNow());
            var filePath = new Java.IO.File(folder, fileName);

            record.OnSaveImage(filePath.AbsolutePath, new UpdateCallback());
            UpdateView();
        }

        private void ShowAbout()
        {
            new AboutDialogFragment().Show(SupportFragmentManager, "About");
        }

        private void UpdateView()
        {
            progressHolder.Visibility = record.WaitMode ? ViewStates.Visible : ViewStates.Gone;

            // Setup image
            imageView.SetImageBitmap(record.DisplayBitmap);

            // Setup image frame
            if (record.ImageMode == MainRecord.ImageState.Source)
            {
                imageFrame.ImageMatrix = imageView.ImageMatrix;
                imageFrame.FramePoints = record.GetDocumentFrame();
                imageFrame.ImageBounds = record.DisplayBitmap != null ? new RectF(imageView.Drawable.Bounds) : null;
                imageFrame.Visibility = ViewStates.Visible;
            }
            else
            {
                imageFrame.Visibility = ViewStates.Gone;
            }

            // NOTE: Using "Special" image mode to prevent spinner blinking
            spnColor.SelectPosition(record.DisplayImageMode == MainRecord.ImageState.Target ? Array.IndexOf(processingItems, record.Processing) + 1 : 0);

            // Setup buttons
            //btnEdit.Visibility = (record.ImageMode != MainRecord.ImageState.InitNothing) ? ViewStates.Visible : ViewStates.Gone;
            spnColor.Visibility = (record.ImageMode != MainRecord.ImageState.InitNothing) ? ViewStates.Visible : ViewStates.Invisible;
            btnSave.Visibility = (record.ImageMode != MainRecord.ImageState.InitNothing) ? ViewStates.Visible : ViewStates.Invisible;

            ShowMessages();
        }

        private void ShowMessages()
        {
            if (record.HasMessages)
            {
                var text = new System.Text.StringBuilder();
                int tail = 0;
                foreach (Message msg in record.WithdrawMessages())
                {
                    string format = msg.Id != 0 ? GetString(msg.Id) : "{0}";                    
                    text.Append(string.Format(format, msg.Arguments));
                    tail = text.Length;
                    text.Append("\r\n");
                }
                text.Remove(tail, text.Length - tail);

                if (text.Length != 0)
                {
                    Snackbar.Make(imageView, text.ToString(), Snackbar.LengthIndefinite).SetAction(Resource.String.action_close, (View view) => { }).Show();
                }
            }
        }
    }
}


