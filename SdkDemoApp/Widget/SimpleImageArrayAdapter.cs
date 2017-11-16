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
using static App.Resource;
using Android.Content.Res;

namespace App.Widget
{
    public class SimpleImageArrayAdapter : ArrayAdapter<Java.Lang.Integer>
    {
        public SimpleImageArrayAdapter(Context context, int [] imagesIds) : base(context, Android.Resource.Layout.SimpleSpinnerItem, convert(imagesIds))
        {
            
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            return GetImageForPosition(position, convertView, parent, DropDownContext);
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            return GetImageForPosition(position, convertView, parent, Context);
        }

        protected virtual View GetImageForPosition(int position, View convertView, ViewGroup parent, Context context)
        {
            ImageView imageView;
            if (convertView != null)
            {
                imageView = (ImageView)convertView;
            }
            else
            {
                imageView = new ImageView(context);
                imageView.LayoutParameters = new AbsListView.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                imageView.SetScaleType(ImageView.ScaleType.FitCenter);
            }
            imageView.SetImageResource((int) GetItem(position));
            return imageView;
        }

        private static Java.Lang.Integer [] convert(int [] source)
        {
            var target = new Java.Lang.Integer[source.Length];
            for (int i = 0; i < source.Length; ++i)
            {
                target[i] = new Java.Lang.Integer(source[i]);
            }
            return target;
        }

        private Context dropDownContext;

        public override Resources.Theme DropDownViewTheme
        {
            get => base.DropDownViewTheme; set
            {
                base.DropDownViewTheme = value;
                if (value == null || value == Context.Theme)
                {
                    dropDownContext = null;
                }
                else
                {
                    dropDownContext = new Android.Support.V7.View.ContextThemeWrapper(Context, value);
                }

            }
        }

        public Context DropDownContext
        {
            get
            {
                return dropDownContext != null ? dropDownContext : Context;
            }

        }
    }
}