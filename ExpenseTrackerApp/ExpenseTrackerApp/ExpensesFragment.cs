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

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeAsync(view);
#pragma warning restore CS4014

            return view;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            _destroyCancellationSource?.Cancel();
        }

        private async Task InitializeAsync(View view)
        {
            if (_destroyCancellationSource.IsCancellationRequested)
                return;

            var listView = view.FindViewById<ListView>(Resource.Id.ExpensesListView);
            var progressBar = view.FindViewById<ProgressBar>(Resource.Id.ExpensesProgressBar);
            var progressText = view.FindViewById<TextView>(Resource.Id.ExpensesProgressText);
            var addButton = view.FindViewById<ImageButton>(Resource.Id.AddExpenseButton);

            progressText.Text = GetString(Resource.String.RetrievingExpenses);

            List<ExpenseItem> expenseItems;

            try
            {
                expenseItems = await _persistedDataFragment.GetExpenseItemsAsync();
            }
            catch (Exception ex)
            {
                if (_destroyCancellationSource.IsCancellationRequested)
                    return;

                var alert = new AlertDialog.Builder(Context).Create();
                alert.SetMessage(ex.Message);

                alert.Show();
                return;
            }

            if (_destroyCancellationSource.IsCancellationRequested)
                return;

            progressBar.Visibility = ViewStates.Gone;
            progressText.Visibility = ViewStates.Gone;

            var expenseItemStrings = expenseItems.Select(item => $"{item.Amount}: {item.Description}");
            listView.Adapter = new ArrayAdapter<string>(Context, Android.Resource.Layout.SimpleListItem1, expenseItemStrings.ToArray());

            addButton.Visibility = ViewStates.Visible;
            addButton.Click += OnAddButtonClick;
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            var intent = new Intent(View.Context, typeof(AddExpenseActivity));
            View.Context.StartActivity(intent);
        }
    }
}