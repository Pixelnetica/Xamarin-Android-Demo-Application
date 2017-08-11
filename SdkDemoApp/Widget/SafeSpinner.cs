using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Support.V7.Widget;
using Android.Content.Res;

namespace App.Widget
{
    // Spinner differentiate between user selected and prorammatically selected
    public class SafeSpinner : AppCompatSpinner, AdapterView.IOnItemSelectedListener
    {

        public delegate void OnItemUserSelected(Spinner spinner, int position, long id);
        private OnItemUserSelected callback;

        private bool userClicked = false;

        public SafeSpinner(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public SafeSpinner(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        public SafeSpinner(Context context, IAttributeSet attrs, int defStyle, int mode) :
            base(context, attrs, defStyle, mode)
        {
            Initialize();
        }

        public SafeSpinner(Context context, IAttributeSet attrs, int defStyle, int mode, Resources.Theme popupTheme) :
            base(context, attrs, defStyle, mode, popupTheme)
        {
            Initialize();
        }

        private void Initialize()
        {
            base.OnItemSelectedListener = this;
        }

        public void OnItemSelected(AdapterView parent, View view, int position, long id)
        {
            if (userClicked)
            {
                callback((Spinner)parent, position, id);
            }
            userClicked = false;
        }

        public void OnNothingSelected(AdapterView parent)
        {
            if (userClicked)
            {
                callback((Spinner)parent, -1, 0);
            }
            userClicked = false;
        }

        public OnItemUserSelected ItemUserSelected { get => callback; set => callback = value; }
        public void SelectPosition(int position)
        {
            if (position != SelectedItemPosition)
            {
                SetSelection(position);
            }
        }

        /// <summary>
        /// Restore spinner background overriden by parent theme or style
        /// </summary>
        /// <param name="context"></param>
        public void RestoreStyleBackground(Context context)
        {
            var attribute = new TypedValue();
            context.Theme.ResolveAttribute(Android.Resource.Attribute.SpinnerStyle, attribute, true);

            var array = context.ObtainStyledAttributes(attribute.ResourceId, new int[] { Android.Resource.Attribute.Background });
            Background = array.GetDrawable(0);
            array.Recycle();
        }

        public override bool PerformClick()
        {
            userClicked = true;
            return base.PerformClick();
        }
    }
}