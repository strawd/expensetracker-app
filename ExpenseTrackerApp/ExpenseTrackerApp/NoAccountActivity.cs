// Copyright 2016 David Straw

using Android.App;
using Android.OS;

namespace ExpenseTrackerApp
{
    [Activity(Label = "@string/NoAccountTitle")]
    public class NoAccountActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.NoAccount);
        }
    }
}