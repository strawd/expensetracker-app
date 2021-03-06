// Copyright 2016 David Straw

using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;

namespace ExpenseTrackerApp
{
    class CheckableLinearLayout : LinearLayout, ICheckable
    {
        static readonly int[] CheckedState = { Android.Resource.Attribute.StatePressed };

        bool _checked;

        public CheckableLinearLayout(Context context)
            : base(context)
        {
        }

        public CheckableLinearLayout(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
        }

        public CheckableLinearLayout(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
        }

        public CheckableLinearLayout(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes)
            : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected CheckableLinearLayout(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                _checked = value;
                RefreshDrawableState();
            }
        }

        public void Toggle()
        {
            Checked = !Checked;
        }

        protected override int[] OnCreateDrawableState(int extraSpace)
        {
            int[] drawableState = base.OnCreateDrawableState(extraSpace + 1);
            if (_checked)
                MergeDrawableStates(drawableState, CheckedState);

            return drawableState;
        }
    }
}