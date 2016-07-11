// Copyright 2016 David Straw

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace ExpenseTrackerApp
{
    [Activity(Label = "@string/AddExpensePeriod")]
    public class AddOrEditExpensePeriodActivity : Activity
    {
        public const string AmountAvailableInCentsKey = "AddOrEditExpensePeriodAmountAvailableInCents";
        public const string StartDateInTicksKey = "AddOrEditExpensePeriodStartDateInTicks";
        public const string ItemIdKey = "AddOrEditExpensePeriodItemId";

        string _editItemId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.AddOrEditExpensePeriod);

            var amountAvailableText = FindViewById<EditText>(Resource.Id.AddExpensePeriodAmountAvailableText);
            var startDatePicker = FindViewById<DatePicker>(Resource.Id.AddExpensePeriodStartDatePicker);

            _editItemId = Intent.GetStringExtra(ItemIdKey);
            if (!string.IsNullOrEmpty(_editItemId))
            {
                // This is an edit, because existing values were sent to the activity
                Title = GetString(Resource.String.EditExpensePeriod);

                int amountAvailableInCents = Intent.GetIntExtra(AmountAvailableInCentsKey, 0);
                long startDateInTicks = Intent.GetLongExtra(StartDateInTicksKey, 0L);

                amountAvailableText.Text = (amountAvailableInCents / 100m).ToString("f2");
                startDatePicker.DateTime = new DateTime(startDateInTicks, DateTimeKind.Local);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (!string.IsNullOrEmpty(_editItemId))
                MenuInflater.Inflate(Resource.Menu.EditExpensePeriodMenu, menu);
            else
                MenuInflater.Inflate(Resource.Menu.AddExpensePeriodMenu, menu);
            return true;
        }

        public override bool OnMenuItemSelected(int featureId, IMenuItem item)
        {
            if (item.ItemId == Resource.Id.AddExpensePeriodMenuItem || item.ItemId == Resource.Id.EditExpensePeriodDoneMenuItem)
            {
                AddOrEditExpensePeriod();
                return true;
            }

            return base.OnMenuItemSelected(featureId, item);
        }

        private void AddOrEditExpensePeriod()
        {
            var amountAvailableText = FindViewById<EditText>(Resource.Id.AddExpensePeriodAmountAvailableText);
            var startDatePicker = FindViewById<DatePicker>(Resource.Id.AddExpensePeriodStartDatePicker);

            decimal amountAvailable;
            if (!decimal.TryParse(amountAvailableText.Text, out amountAvailable) || amountAvailable <= 0m || amountAvailable > 1000000m)
            {
                ShowValidationError(GetString(Resource.String.AmountAvailableValidationMessage));
                return;
            }

            var resultIntent = new Intent();
            resultIntent.PutExtra(AmountAvailableInCentsKey, (int)(amountAvailable * 100m));
            resultIntent.PutExtra(StartDateInTicksKey, startDatePicker.DateTime.Ticks);

            if (!string.IsNullOrEmpty(_editItemId))
                resultIntent.PutExtra(ItemIdKey, _editItemId);

            SetResult(Result.Ok, resultIntent);

            Finish();
        }

        private void ShowValidationError(string message)
        {
            var alert = new AlertDialog.Builder(this).Create();
            alert.SetButton(GetString(Resource.String.Close), (sender, e) => alert.Cancel());
            alert.SetMessage(message);

            alert.Show();
        }
    }
}