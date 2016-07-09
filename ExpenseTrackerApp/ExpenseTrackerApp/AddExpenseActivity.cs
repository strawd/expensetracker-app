// Copyright 2016 David Straw

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace ExpenseTrackerApp
{
    [Activity(Label = "@string/AddExpense")]
    public class AddExpenseActivity : Activity
    {
        public const string AmountInCentsKey = "AddExpenseAmountInCents";
        public const string DescriptionKey = "AddExpenseDescription";
        public const string DateInTicksKey = "AddExpenseDateInTicks";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.AddExpense);

            var descriptionText = FindViewById<EditText>(Resource.Id.AddExpenseDescriptionText);
            descriptionText.EditorAction += OnDescriptionTextEditorAction;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.AddExpenseMenu, menu);
            return true;
        }

        public override bool OnMenuItemSelected(int featureId, IMenuItem item)
        {
            if (item.ItemId == Resource.Id.AddExpenseMenuItem)
            {
                AddExpenseItem();
                return true;
            }

            return base.OnMenuItemSelected(featureId, item);
        }

        private void OnDescriptionTextEditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Send)
            {
                AddExpenseItem();
            }
        }

        private void AddExpenseItem()
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