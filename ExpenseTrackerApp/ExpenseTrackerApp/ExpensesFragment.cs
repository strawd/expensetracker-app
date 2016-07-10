// Copyright 2016 David Straw

using System;
using System.Collections.Generic;
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
        ActionMode _actionMode;

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
            var listView = view.FindViewById<ListView>(Resource.Id.ExpensesListView);

            addButton.Click += OnAddButtonClick;
            listView.ItemClick += OnListViewItemClick;

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeExpenseItemsAsync(view);
#pragma warning restore CS4014

            return view;
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

            if (_actionMode != null)
            {
                _actionMode.Finish();
                _actionMode = null;
            }

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

            listView.Adapter = new ExpensesAdapter(Activity, expenseItems);

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
                long dateInTicks = data.GetLongExtra(AddExpenseActivity.DateInTicksKey, 0);

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

            await _persistedDataFragment.DeleteExpenseItemAsync(expenseItem);

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
                    // TODO
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