// Copyright 2016 David Straw

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;

namespace ExpenseTrackerApp
{
    public class ExpensesFragment : Android.Support.V4.App.Fragment
    {
        const int AddExpenseRequestCode = 1;
        const int EditExpenseRequestCode = 2;

        PersistedDataFragment _persistedDataFragment;
        CancellationTokenSource _destroyCancellationSource;
        ActionMode _actionMode;
        bool _refreshing = false;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _persistedDataFragment = (PersistedDataFragment)FragmentManager.FindFragmentByTag(MainActivity.PersistedDataFragmentTag);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _destroyCancellationSource?.Dispose();
            _destroyCancellationSource = new CancellationTokenSource();

            var view = inflater.Inflate(Resource.Layout.Expenses, container, false);

            var addButton = view.FindViewById<ImageButton>(Resource.Id.AddExpenseButton);
            var listView = view.FindViewById<ListView>(Resource.Id.ExpensesListView);
            var refreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.ExpensesRefreshLayout);

            addButton.Click += OnAddButtonClick;
            listView.ItemClick += OnListViewItemClick;
            refreshLayout.Refresh += OnRefreshLayoutRefresh;

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeExpenseItemsAsync(view);
#pragma warning restore CS4014

            return view;
        }

        public void FinishActionMode()
        {
            if (_actionMode != null)
            {
                _actionMode.Finish();
                _actionMode = null;
            }
        }

        private void OnListViewItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var listView = (ListView)e.Parent;

            if (listView.CheckedItemCount > 0 && _actionMode == null)
            {
                _actionMode = Activity.StartActionMode(new ActionModeCallback(this));
            }
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            FinishActionMode();

            _destroyCancellationSource?.Cancel();
            _refreshing = false;
        }

        private async Task InitializeExpenseItemsAsync(View view)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;
            _refreshing = true;

            var listView = view.FindViewById<ListView>(Resource.Id.ExpensesListView);
            var progressBar = view.FindViewById<ProgressBar>(Resource.Id.ExpensesProgressBar);
            var progressText = view.FindViewById<TextView>(Resource.Id.ExpensesProgressText);
            var addButton = view.FindViewById<ImageButton>(Resource.Id.AddExpenseButton);
            var refreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.ExpensesRefreshLayout);

            progressBar.Visibility = ViewStates.Visible;
            progressText.Visibility = ViewStates.Visible;
            addButton.Visibility = ViewStates.Invisible;

            progressText.Text = GetString(Resource.String.RetrievingExpenses);

            List<ExpenseItem> expenseItems;

            try
            {
                expenseItems = await _persistedDataFragment.GetExpenseItemsAsync(Context, localDestroyCancellationSource.Token);
            }
            catch (Exception ex)
            {
                _persistedDataFragment.InvalidateExpenseItems();

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

            listView.Adapter = new ExpensesAdapter(Activity, expenseItems);

            addButton.Visibility = ViewStates.Visible;

            refreshLayout.Refreshing = false;
            _refreshing = false;
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            var intent = new Intent(View.Context, typeof(AddOrEditExpenseActivity));
            StartActivityForResult(intent, AddExpenseRequestCode);
        }

        private void OnRefreshLayoutRefresh(object sender, EventArgs e)
        {
            var refreshLayout = View.FindViewById<SwipeRefreshLayout>(Resource.Id.ExpensesRefreshLayout);

            if (_refreshing)
            {
                refreshLayout.Refreshing = false;
                return;
            }

            _persistedDataFragment.InvalidateExpenseItems();

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeExpenseItemsAsync(View);
#pragma warning restore CS4014
        }

        public override async void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == AddExpenseRequestCode && resultCode == (int)Result.Ok)
            {
                await InsertExpenseItemAsync(data);
            }
            else if (requestCode == EditExpenseRequestCode && resultCode == (int)Result.Ok)
            {
                await UpdateExpenseItemAsync(data);
            }

            FinishActionMode();
        }

        private async Task InsertExpenseItemAsync(Intent data)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            int amountInCents = data.GetIntExtra(AddOrEditExpenseActivity.AmountInCentsKey, 0);
            string description = data.GetStringExtra(AddOrEditExpenseActivity.DescriptionKey);
            long dateInTicks = data.GetLongExtra(AddOrEditExpenseActivity.DateInTicksKey, 0);

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
                await _persistedDataFragment.InsertExpenseItemAsync(Context, expenseItem, localDestroyCancellationSource.Token);
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

        private async Task UpdateExpenseItemAsync(Intent data)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            string itemId = data.GetStringExtra(AddOrEditExpenseActivity.ItemIdKey);
            int amountInCents = data.GetIntExtra(AddOrEditExpenseActivity.AmountInCentsKey, 0);
            string description = data.GetStringExtra(AddOrEditExpenseActivity.DescriptionKey);
            long dateInTicks = data.GetLongExtra(AddOrEditExpenseActivity.DateInTicksKey, 0);

            var progressDialog = new ProgressDialog(Context);
            progressDialog.Indeterminate = true;
            progressDialog.SetTitle(Resource.String.UpdatingExpense);
            progressDialog.Show();

            var expenseItem = new ExpenseItem
            {
                Id = itemId,
                Amount = amountInCents / 100m,
                Description = description,
                Date = new DateTimeOffset(new DateTime(dateInTicks, DateTimeKind.Local))
            };

            try
            {
                await _persistedDataFragment.UpdateExpenseItemAsync(Context, expenseItem, localDestroyCancellationSource.Token);
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

        private void EditSelectedExpense()
        {
            var listView = View.FindViewById<ListView>(Resource.Id.ExpensesListView);
            var adapter = listView.Adapter as ExpensesAdapter;

            if (listView.CheckedItemCount > 0 && adapter != null)
            {
                ExpenseItem selectedExpense = adapter[listView.CheckedItemPosition];

                var intent = new Intent(View.Context, typeof(AddOrEditExpenseActivity));

                intent.PutExtra(AddOrEditExpenseActivity.ItemIdKey, selectedExpense.Id);
                intent.PutExtra(AddOrEditExpenseActivity.AmountInCentsKey, (int)(selectedExpense.Amount * 100m));
                intent.PutExtra(AddOrEditExpenseActivity.DescriptionKey, selectedExpense.Description);
                intent.PutExtra(AddOrEditExpenseActivity.DateInTicksKey, selectedExpense.Date.LocalDateTime.Ticks);

                StartActivityForResult(intent, EditExpenseRequestCode);
            }
        }

        private void DeleteSelectedExpense()
        {
            var listView = View.FindViewById<ListView>(Resource.Id.ExpensesListView);
            var adapter = listView.Adapter as ExpensesAdapter;

            if (listView.CheckedItemCount > 0 && adapter != null)
            {
                ExpenseItem selectedExpense = adapter[listView.CheckedItemPosition];

                var alert = new AlertDialog.Builder(Context).Create();
                alert.SetMessage(string.Format(GetString(Resource.String.DeleteExpenseConfirmation), selectedExpense.Description));
                alert.SetButton(
                    GetString(Resource.String.DeleteExpenseCommand),
                    async (sender, e) => { await ExecuteDeleteExpenseAsync(selectedExpense); });
                alert.SetButton2(
                    GetString(Android.Resource.String.Cancel),
                    (sender, e) => { alert.Cancel(); });

                alert.Show();
            }
        }

        private async Task ExecuteDeleteExpenseAsync(ExpenseItem expenseItem)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            var progressDialog = new ProgressDialog(Context);
            progressDialog.Indeterminate = true;
            progressDialog.SetTitle(Resource.String.DeletingExpense);
            progressDialog.Show();

            await _persistedDataFragment.DeleteExpenseItemAsync(Context, expenseItem, localDestroyCancellationSource.Token);

            _persistedDataFragment.InvalidateExpenseItems();

            if (localDestroyCancellationSource.IsCancellationRequested)
                return;

            progressDialog.Hide();

            await InitializeExpenseItemsAsync(View);
        }

        private class ActionModeCallback : Java.Lang.Object, ActionMode.ICallback
        {
            ExpensesFragment _expensesFragment;

            public ActionModeCallback(ExpensesFragment expensesFragment)
            {
                _expensesFragment = expensesFragment;
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                if (item.ItemId == Resource.Id.EditExpenseMenuItem)
                {
                    _expensesFragment.EditSelectedExpense();

                    return true;
                }
                if (item.ItemId == Resource.Id.DeleteExpenseMenuItem)
                {
                    _expensesFragment.DeleteSelectedExpense();

                    return true;
                }
                return false;
            }

            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.ExpenseItemMenu, menu);
                return true;
            }

            public void OnDestroyActionMode(ActionMode mode)
            {
                _expensesFragment._actionMode = null;

                var listView = _expensesFragment.View.FindViewById<ListView>(Resource.Id.ExpensesListView);

                if (listView.CheckedItemCount > 0)
                    listView.SetItemChecked(listView.CheckedItemPosition, false);
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                return false;
            }
        }
    }
}