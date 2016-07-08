// Copyright 2016 David Straw

using Android.App;
using Android.OS;

namespace ExpenseTrackerApp
{
    [Activity(Label = "@string/AddExpense")]
    public class AddExpenseActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.AddExpense);
        }
    }
}