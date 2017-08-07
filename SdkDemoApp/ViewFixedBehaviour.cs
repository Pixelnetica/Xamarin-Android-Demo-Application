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
using Android.Support.Design.Widget;
using Android.Util;
using Java.Lang;
using Android.Support.V4.View;
using Android.Graphics;

namespace App
{
#if XXX
    public class ViewOffsetBehaviour : CoordinatorLayout.Behavior
    {
        private ViewOffsetHelper mViewOffsetHelper;

        private int mTempTopBottomOffset = 0;
        private int mTempLeftRightOffset = 0;

        public ViewOffsetBehaviour()
        {
            
        }

        public ViewOffsetBehaviour(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }

        public override bool OnLayoutChild(CoordinatorLayout parent, Java.Lang.Object child, int layoutDirection)
        {
            // First let lay the child out
            DoLayoutChild(parent, (View)child, layoutDirection);

            if (mViewOffsetHelper == null)
            {
                mViewOffsetHelper = new ViewOffsetHelper((View)child);
            }
            mViewOffsetHelper.onViewLayout();

            if (mTempTopBottomOffset != 0)
            {
                mViewOffsetHelper.setTopAndBottomOffset(mTempTopBottomOffset);
                mTempTopBottomOffset = 0;
            }
            if (mTempLeftRightOffset != 0)
            {
                mViewOffsetHelper.setLeftAndRightOffset(mTempLeftRightOffset);
                mTempLeftRightOffset = 0;
            }

            return true;
        }
        protected virtual void DoLayoutChild(CoordinatorLayout parent, View child, int layoutDirection)
        {
            // Let the parent lay it out by default
            parent.OnLayoutChild(child, layoutDirection);
        }

        public virtual bool SetTopAndBottomOffset(int offset)
        {
            if (mViewOffsetHelper != null)
            {
                return mViewOffsetHelper.setTopAndBottomOffset(offset);
            }
            else
            {
                mTempTopBottomOffset = offset;
            }
            return false;
        }

        public bool SetLeftAndRightOffset(int offset)
        {
            if (mViewOffsetHelper != null)
            {
                return mViewOffsetHelper.setLeftAndRightOffset(offset);
            }
            else
            {
                mTempLeftRightOffset = offset;
            }
            return false;
        }

        public int GetTopAndBottomOffset()
        {
            return mViewOffsetHelper != null ? mViewOffsetHelper.getTopAndBottomOffset() : 0;
        }

        public int GetLeftAndRightOffset()
        {
            return mViewOffsetHelper != null ? mViewOffsetHelper.getLeftAndRightOffset() : 0;
        }
    }

    class ViewOffsetHelper
    {

        private readonly View mView;

        private int mLayoutTop;
        private int mLayoutLeft;
        private int mOffsetTop;
        private int mOffsetLeft;

        public ViewOffsetHelper(View view)
        {
            this.mView = view;
        }

        public void onViewLayout()
        {
            // Now grab the intended top
            mLayoutTop = mView.Top;
            mLayoutLeft = mView.Left;

            // And offset it as needed
            updateOffsets();
        }

        private void updateOffsets()
        {
            ViewCompat.OffsetTopAndBottom(mView, mOffsetTop - (mView.Top - mLayoutTop));
            ViewCompat.OffsetLeftAndRight(mView, mOffsetLeft - (mView.Left - mLayoutLeft));
        }

        /**
         * Set the top and bottom offset for this {@link ViewOffsetHelper}'s view.
         *
         * @param offset the offset in px.
         * @return true if the offset has changed
         */
        public bool setTopAndBottomOffset(int offset)
        {
            if (mOffsetTop != offset)
            {
                mOffsetTop = offset;
                updateOffsets();
                return true;
            }
            return false;
        }

        /**
         * Set the left and right offset for this {@link ViewOffsetHelper}'s view.
         *
         * @param offset the offset in px.
         * @return true if the offset has changed
         */
        public bool setLeftAndRightOffset(int offset)
        {
            if (mOffsetLeft != offset)
            {
                mOffsetLeft = offset;
                updateOffsets();
                return true;
            }
            return false;
        }

        public int getTopAndBottomOffset()
        {
            return mOffsetTop;
        }

        public int getLeftAndRightOffset()
        {
            return mOffsetLeft;
        }

        public int getLayoutTop()
        {
            return mLayoutTop;
        }

        public int getLayoutLeft()
        {
            return mLayoutLeft;
        }
    }

    public abstract class HeaderScrollingViewBehavior: ViewOffsetBehavior
    {

        readonly Rect mTempRect1 = new Rect();
        readonly Rect mTempRect2 = new Rect();

        private int mVerticalLayoutGap = 0;
        private int mOverlayTop;

        public HeaderScrollingViewBehavior() { }

        public HeaderScrollingViewBehavior(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }

        override public bool OnMeasureChild(CoordinatorLayout parent, View child,
            int parentWidthMeasureSpec, int widthUsed, int parentHeightMeasureSpec,
            int heightUsed)
        {
            int childLpHeight = child.LayoutParameters.Height;
            if (childLpHeight == ViewGroup.LayoutParams.MatchParent
                || childLpHeight == ViewGroup.LayoutParams.WrapContent)
            {
                // If the menu's height is set to match_parent/wrap_content then measure it
                // with the maximum visible height

                IList<View> dependencies = parent.GetDependencies(child);
                View header = findFirstDependency(dependencies);
                if (header != null)
                {
                    if (ViewCompat.GetFitsSystemWindows(header)
                            && !ViewCompat.GetFitsSystemWindows(child))
                    {
                        // If the header is fitting system windows then we need to also,
                        // otherwise we'll get CoL's compatible measuring
                        ViewCompat.SetFitsSystemWindows(child, true);

                        if (ViewCompat.GetFitsSystemWindows(child))
                        {
                            // If the set succeeded, trigger a new layout and return true
                            child.RequestLayout();
                            return true;
                        }
                    }

                    int availableHeight = View.MeasureSpec.GetSize(parentHeightMeasureSpec);
                    if (availableHeight == 0)
                    {
                        // If the measure spec doesn't specify a size, use the current height
                        availableHeight = parent.Height;
                    }

                    int height = availableHeight - header.MeasuredHeight
                            + GetScrollRange(header);
                    int heightMeasureSpec = View.MeasureSpec.MakeMeasureSpec(height,
                            childLpHeight == ViewGroup.LayoutParams.MatchParent
                                   ? MeasureSpecMode.Exactly
                                  : MeasureSpecMode.AtMost);

                    // Now measure the scrolling view with the correct height
                    parent.OnMeasureChild(child, parentWidthMeasureSpec,
                            widthUsed, heightMeasureSpec, heightUsed);

                    return true;
                }
            }
            return false;
        }

        protected override void LayoutChild(CoordinatorLayout parent, Java.Lang.Object c,
                int layoutDirection)
        {
            View child = (View)c;
            IList<View> dependencies = parent.GetDependencies(child);
            View header = findFirstDependency(dependencies);

            if (header != null)
            {
                CoordinatorLayout.LayoutParams lp =
                        (CoordinatorLayout.LayoutParams)child.LayoutParameters;
                Rect available = mTempRect1;
                available.Set(parent.PaddingLeft + lp.LeftMargin,
                        header.Bottom + lp.TopMargin,
                        parent.Width - parent.PaddingRight - lp.RightMargin,
                        parent.Height + header.Bottom
                            - parent.PaddingBottom - lp.BottomMargin);

                WindowInsetsCompat parentInsets = parent.getLastWindowInsets();
                if (parentInsets != null && ViewCompat.GetFitsSystemWindows(parent)
                        && !ViewCompat.GetFitsSystemWindows(child))
                {
                    // If we're set to handle insets but this child isn't, then it has been measured as
                    // if there are no insets. We need to lay it out to match horizontally.
                    // Top and bottom and already handled in the logic above
                    available.Left += parentInsets.SystemWindowInsetLeft;
                    available.Right -= parentInsets.SystemWindowInsetRight;
                }

                Rect @out = mTempRect2;
                GravityCompat.Apply(resolveGravity(lp.Gravity), child.MeasuredWidth,
                        child.MeasuredHeight, available, @out, layoutDirection);

                int overlap = getOverlapPixelsForOffset(header);

                child.Layout(@out.Left, @out.Top - overlap, @out.Right, @out.Bottom - overlap);
                mVerticalLayoutGap = @out.Top - header.Bottom;
            }
            else
            {
                // If we don't have a dependency, let super handle it
                base.LayoutChild(parent, child, layoutDirection);
                mVerticalLayoutGap = 0;
            }
        }

        float getOverlapRatioForOffset(View header)
        {
            return 1f;
        }

        static int constrain(int amount, int low, int high)
        {
            return amount < low ? low : (amount > high ? high : amount);
        }

        int getOverlapPixelsForOffset(View header)
        {
            return mOverlayTop == 0 ? 0 : constrain(
                    (int)(getOverlapRatioForOffset(header) * mOverlayTop), 0, mOverlayTop);
        }

        private static int resolveGravity(int gravity)
        {
            return gravity == (int)GravityFlags.NoGravity ? (int)GravityCompat.Start | (int)GravityFlags.Top : gravity;
        }

        protected abstract View findFirstDependency(IList<View> views);

        int GetScrollRange(View v)
        {
            return v.MeasuredHeight;
        }

        /**
        * The gap between the top of the scrolling view and the bottom of the header layout in pixels.
        */
        int GetVerticalLayoutGap()
        {
            return mVerticalLayoutGap;
        }

        /**
        * Set the distance that this view should overlap any {@link AppBarLayout}.
        *
        * @param overlayTop the distance in px
        */
        public void SetOverlayTop(int overlayTop)
        {
            mOverlayTop = overlayTop;
        }

        /**
        * Returns the distance that this view should overlap any {@link AppBarLayout}.
        */
        public int GetOverlayTop()
        {
            return mOverlayTop;
        }
    }
#endif

    public class ViewFixedBehaviour : AppBarLayout.ScrollingViewBehavior
    {
        public ViewFixedBehaviour()
        {

        }

        public ViewFixedBehaviour(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }
    }
}