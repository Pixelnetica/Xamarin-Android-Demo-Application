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

namespace App.Widget
{
    class ImageTextSpinnerAdapter : ArrayAdapter<ImageTextSpinnerAdapter.Item>
    {
        public class Item : Tuple<int, int>
        {
            public Item(int imageId, int textId) : base(imageId, textId)
            {

            }

            public int ImageId { get => Item1; }
            public int TextId { get => Item2; }

        }
        public ImageTextSpinnerAdapter(Context context, Item[] data) : base(context, Resource.Layout.ImageTextSpinnerItem, Android.Resource.Id.Text1, data)
        {

        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = base.GetView(position, convertView, parent);
            var item = GetItem(position);
            view.FindViewById<ImageView>(Resource.Id.image1).SetImageResource(item.ImageId);
            view.FindViewById<TextView>(Android.Resource.Id.Text1).SetText(item.TextId);
            return view;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var view = base.GetDropDownView(position, convertView, parent);
            var item = GetItem(position);
            view.FindViewById<ImageView>(Resource.Id.image1).SetImageResource(item.ImageId);
            view.FindViewById<TextView>(Android.Resource.Id.Text1).SetText(item.TextId);
            return view;
        }
    }
}