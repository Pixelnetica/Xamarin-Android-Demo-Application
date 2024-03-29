﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace App.Main
{
    [Activity(Label = "EditActivity", Icon = "@drawable/icon", Theme = "@style/AppTheme")]
    public class EditActivity : Utils.BaseActivity
    {

        ImageView imageView;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Edit);
            SetupContentLayout();

            imageView = FindViewById<ImageView>(Resource.Id.image_holder);
        }
    }
}