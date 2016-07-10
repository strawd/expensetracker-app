// Copyright 2016 David Straw

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace ExpenseTrackerApp
{
    [Activity(Label = "@string/AddExpense")]
    public class AddOrEditExpenseActivity : Activity
    {
        public const string AmountInCentsKey = "AddOrEditExpenseAmountInCents";
        public const string DescriptionKey = "AddOrEditExpenseDescription";
        public const string DateInTicksKey = "AddOrEditExpenseDateInTicks";
        public const string ItemIdKey = "AddOrEditExpenseItemId";

        string _editItemId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.AddOrEditExpense);

            var amountText = FindViewById<EditText>(Resource.Id.AddExpenseAmountText);
            var descriptionText = FindViewById<EditText>(Resource.Id.AddExpenseDescriptionText);
            var datePicker = FindViewById<DatePicker>(Resource.Id.AddExpenseDatePicker);

            _editItemId = Intent.GetStringExtra(ItemIdKey);
            if (!string.IsNullOrEmpty(_editItemId))
            {
                // This is an edit, because existing values were sent to the activity
                Title = GetString(Resource.String.EditExpense);

                int amountInCents = Intent.GetIntExtra(AmountInCentsKey, 0);
                string description = Intent.GetStringExtra(DescriptionKey);
                long dateInTicks = Intent.GetLongExtra(DateInTicksKey, 0L);

                amountText.Text = (amountInCents / 100m).ToString("f2");
                descriptionText.Text = description;
                datePicker.DateTime = new DateTime(dateInTicks, System.DateTimeKind.Local);
            }

            descriptionText.EditorAction += OnDescriptionTextEditorAction;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (!string.IsNullOrEmpty(_editItemId))
                MenuInflater.Inflate(Resource.Menu.EditExpenseMenu, menu);
            else
                MenuInflater.Inflate(Resource.Menu.AddExpenseMenu, menu);
            return true;
        }

        public override bool OnMenuItemSelected(int featureId, IMenuItem item)
        {
            if (item.ItemId == Resource.Id.AddExpenseMenuItem || item.ItemId == Resource.Id.EditExpenseDoneMenuItem)
            {
                AddOrEditExpenseItem();
                return true;
            }

            return base.OnMenuItemSelected(featureId, item);
        }

        private void OnDescriptionTextEditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Send)
            {
                AddOrEditExpenseItem();
            }
        }

        private void AddOrEditExpenseItem()
        {
            var amountText = FindViewById<EditText>(Resource.Id.AddExpenseAmountText);
            var descriptionText = FindViewById<EditText>(Resource.Id.AddExpenseDescriptionText);
            var datePicker = FindViewById<DatePicker>(Resource.Id.AddExpenseDatePicker);

            decimal amount;
            if (!decimal.TryParse(amountText.Text, out amount) || amount <= 0m || amount > 1000000m)
            {
                ShowValidationError(GetString(Resource.String.AmountValidationMessage));
                return;
            }

            string description = descriptionText.Text?.Trim();
            if (string.IsNullOrEmpty(description) || description.Length > 200)
            {
                ShowValidationError(GetString(Resource.String.DescriptionValidationMessage));
                return;
            }

            var resultIntent = new Intent();
            resultIntent.PutExtra(AmountInCentsKey, (int)(amount * 100m));
            resultIntent.PutExtra(DescriptionKey, description);
            resultIntent.PutExtra(DateInTicksKey, datePicker.DateTime.Ticks);

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