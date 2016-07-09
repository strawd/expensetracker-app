// Copyright 2016 David Straw

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;

namespace ExpenseTrackerApp
{
    public class ExpensesFragment : Fragment
    {
        const int AddExpenseRequestCode = 1;

        PersistedDataFragment _persistedDataFragment;
        CancellationTokenSource _destroyCancellationSource;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _persistedDataFragment = FragmentManager.FindFragmentByTag<PersistedDataFragment>(MainActivity.PersistedDataFragmentTag);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _destroyCancellationSource?.Dispose();
            _destroyCancellationSource = new CancellationTokenSource();

            var view = inflater.Inflate(Resource.Layout.Expenses, container, false);

            var addButton = view.FindViewById<ImageButton>(Resource.Id.AddExpenseButton);
            addButton.Click += OnAddButtonClick;

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeExpenseItemsAsync(view);
#pragma warning restore CS4014

            return view;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            _destroyCancellationSource?.Cancel();
        }

        private async Task InitializeExpenseItemsAsync(View view)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            var listView = view.FindViewById<ListView>(Resource.Id.ExpensesListView);
            var progressBar = view.FindViewById<ProgressBar>(Resource.Id.ExpensesProgressBar);
            var progressText = view.FindViewById<TextView>(Resource.Id.ExpensesProgressText);
            var addButton = view.FindViewById<ImageButton>(Resource.Id.AddExpenseButton);

            progressBar.Visibility = ViewStates.Visible;
            progressText.Visibility = ViewStates.Visible;
            addButton.Visibility = ViewStates.Invisible;

            progressText.Text = GetString(Resource.String.RetrievingExpenses);

            List<ExpenseItem> expenseItems;

            try
            {
                expenseItems = await _persistedDataFragment.GetExpenseItemsAsync();
            }
            catch (Exception ex)
            {
                if (localDestroyCancellationSource.IsCancellationRequested)
                    return;

                var alert = new AlertDialog.Builder(Context).Create();
                alert.SetMessage(ex.Message);

                alert.Show();
                return;
            }

            if (localDestroyCancellationSource.IsCancellationRequested)
                return;

            progressBar.Visibility = ViewStates.Gone;
            progressText.Visibility = ViewStates.Gone;

            var expenseItemStrings = expenseItems.Select(item => $"{item.Amount}: {item.Description}");
            listView.Adapter = new ArrayAdapter<string>(Context, Android.Resource.Layout.SimpleListItem1, expenseItemStrings.ToArray());

            addButton.Visibility = ViewStates.Visible;
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            var intent = new Intent(View.Context, typeof(AddExpenseActivity));
            StartActivityForResult(intent, AddExpenseRequestCode);
        }

        public override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            var localDestroyCancellationSource = _destroyCancellationSource;

            if (requestCode == AddExpenseRequestCode && resultCode == Result.Ok)
            {
                int amountInCents = data.GetIntExtra(AddExpenseActivity.AmountInCentsKey, 0);
                string description = data.GetStringExtra(AddExpenseActivity.DescriptionKey);
                int dateInTicks = data.GetIntExtra(AddExpenseActivity.DateInTicksKey, 0);

                var progressDialog = new ProgressDialog(Context);
                progressDialog.Indeterminate = true;
                progressDialog.SetTitle(Resource.String.AddingExpense);
                progressDialog.Show();

                var expenseItem = new ExpenseItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Amount = amountInCents / 100m,
                    Description = description,
                    Date = new DateTimeOffset(new DateTime(dateInTicks, DateTimeKind.Local))
                };

                try
                {
                    await _persistedDataFragment.InsertExpenseItemAsync(expenseItem);
                }
                catch (Exception ex)
                {
                    if (localDestroyCancellationSource.IsCancellationRequested)
                        return;

                    progressDialog.Hide();

                    var alert = new AlertDialog.Builder(Context).Create();
                    alert.SetMessage(ex.Message);

                    alert.Show();
                    return;
                }

                _persistedDataFragment.InvalidateExpenseItems();

                if (localDestroyCancellationSource.IsCancellationRequested)
                    return;

                progressDialog.Hide();

                await InitializeExpenseItemsAsync(View);
            }
        }
    }
}