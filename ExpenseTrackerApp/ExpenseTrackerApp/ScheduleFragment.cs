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
    public class ScheduleFragment : Fragment
    {
        const int AddExpensePeriodRequestCode = 1;
        const int EditExpensePeriodRequestCode = 2;

        PersistedDataFragment _persistedDataFragment;
        CancellationTokenSource _destroyCancellationSource;
        ActionMode _actionMode;
        bool _refreshing = false;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _persistedDataFragment = FragmentManager.FindFragmentByTag<PersistedDataFragment>(MainActivity.PersistedDataFragmentTag);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _destroyCancellationSource?.Dispose();
            _destroyCancellationSource = new CancellationTokenSource();

            var view = inflater.Inflate(Resource.Layout.Schedule, container, false);

            var addButton = view.FindViewById<ImageButton>(Resource.Id.AddExpensePeriodButton);
            var listView = view.FindViewById<ListView>(Resource.Id.ExpensePeriodsListView);
            var refreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.ScheduleRefreshLayout);

            addButton.Click += OnAddButtonClick;
            listView.ItemClick += OnListViewItemClick;
            refreshLayout.Refresh += OnRefreshLayoutRefresh;

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeExpensePeriodsAsync(view);
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
            _refreshing = false;
        }

        private async Task InitializeExpensePeriodsAsync(View view)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            var listView = view.FindViewById<ListView>(Resource.Id.ExpensePeriodsListView);
            var progressBar = view.FindViewById<ProgressBar>(Resource.Id.ScheduleProgressBar);
            var progressText = view.FindViewById<TextView>(Resource.Id.ScheduleProgressText);
            var addButton = view.FindViewById<ImageButton>(Resource.Id.AddExpensePeriodButton);
            var refreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.ScheduleRefreshLayout);

            progressBar.Visibility = ViewStates.Visible;
            progressText.Visibility = ViewStates.Visible;
            addButton.Visibility = ViewStates.Invisible;

            progressText.Text = GetString(Resource.String.RetrievingExpensePeriods);

            List<ExpensePeriod> expensePeriods;

            try
            {
                expensePeriods = await _persistedDataFragment.GetExpensePeriodsAsync(Context, localDestroyCancellationSource.Token);
            }
            catch (Exception ex)
            {
                _persistedDataFragment.InvalidateExpensePeriods();

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

            listView.Adapter = new ExpensePeriodsAdapter(Activity, expensePeriods);

            addButton.Visibility = ViewStates.Visible;

            refreshLayout.Refreshing = false;
            _refreshing = false;
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            var intent = new Intent(View.Context, typeof(AddOrEditExpensePeriodActivity));
            StartActivityForResult(intent, AddExpensePeriodRequestCode);
        }

        private void OnRefreshLayoutRefresh(object sender, EventArgs e)
        {
            var refreshLayout = View.FindViewById<SwipeRefreshLayout>(Resource.Id.ScheduleRefreshLayout);

            if (_refreshing)
            {
                refreshLayout.Refreshing = false;
                return;
            }

            _persistedDataFragment.InvalidateExpensePeriods();

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeExpensePeriodsAsync(View);
#pragma warning restore CS4014
        }

        public override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == AddExpensePeriodRequestCode && resultCode == Result.Ok)
            {
                await InsertExpensePeriodAsync(data);
            }
            else if (requestCode == EditExpensePeriodRequestCode && resultCode == Result.Ok)
            {
                await UpdateExpensePeriodAsync(data);
            }

            if (_actionMode != null)
            {
                _actionMode.Finish();
                _actionMode = null;
            }
        }

        private async Task InsertExpensePeriodAsync(Intent data)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            int amountAvailableInCents = data.GetIntExtra(AddOrEditExpensePeriodActivity.AmountAvailableInCentsKey, 0);
            long startDateInTicks = data.GetLongExtra(AddOrEditExpensePeriodActivity.StartDateInTicksKey, 0);

            var progressDialog = new ProgressDialog(Context);
            progressDialog.Indeterminate = true;
            progressDialog.SetTitle(Resource.String.AddingExpensePeriod);
            progressDialog.Show();

            var expensePeriod = new ExpensePeriod
            {
                Id = Guid.NewGuid().ToString(),
                AmountAvailable = amountAvailableInCents / 100m,
                StartDate = new DateTimeOffset(new DateTime(startDateInTicks, DateTimeKind.Local))
            };

            try
            {
                await _persistedDataFragment.InsertExpensePeriodAsync(Context, expensePeriod, localDestroyCancellationSource.Token);
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

            _persistedDataFragment.InvalidateExpensePeriods();

            if (localDestroyCancellationSource.IsCancellationRequested)
                return;

            progressDialog.Hide();

            await InitializeExpensePeriodsAsync(View);
        }

        private async Task UpdateExpensePeriodAsync(Intent data)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            string itemId = data.GetStringExtra(AddOrEditExpensePeriodActivity.ItemIdKey);
            int amountAvailableInCents = data.GetIntExtra(AddOrEditExpensePeriodActivity.AmountAvailableInCentsKey, 0);
            long startDateInTicks = data.GetLongExtra(AddOrEditExpensePeriodActivity.StartDateInTicksKey, 0);

            var progressDialog = new ProgressDialog(Context);
            progressDialog.Indeterminate = true;
            progressDialog.SetTitle(Resource.String.UpdatingExpensePeriod);
            progressDialog.Show();

            var expensePeriod = new ExpensePeriod
            {
                Id = itemId,
                AmountAvailable = amountAvailableInCents / 100m,
                StartDate = new DateTimeOffset(new DateTime(startDateInTicks, DateTimeKind.Local))
            };

            try
            {
                await _persistedDataFragment.UpdateExpensePeriodAsync(Context, expensePeriod, localDestroyCancellationSource.Token);
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

            _persistedDataFragment.InvalidateExpensePeriods();

            if (localDestroyCancellationSource.IsCancellationRequested)
                return;

            progressDialog.Hide();

            await InitializeExpensePeriodsAsync(View);
        }

        private void EditSelectedExpensePeriod()
        {
            var listView = View.FindViewById<ListView>(Resource.Id.ExpensePeriodsListView);
            var adapter = listView.Adapter as ExpensePeriodsAdapter;

            if (listView.CheckedItemCount > 0 && adapter != null)
            {
                ExpensePeriod selectedExpensePeriod = adapter[listView.CheckedItemPosition];

                var intent = new Intent(View.Context, typeof(AddOrEditExpensePeriodActivity));

                intent.PutExtra(AddOrEditExpensePeriodActivity.ItemIdKey, selectedExpensePeriod.Id);
                intent.PutExtra(AddOrEditExpensePeriodActivity.AmountAvailableInCentsKey, (int)(selectedExpensePeriod.AmountAvailable * 100m));
                intent.PutExtra(AddOrEditExpensePeriodActivity.StartDateInTicksKey, selectedExpensePeriod.StartDate.LocalDateTime.Ticks);

                StartActivityForResult(intent, EditExpensePeriodRequestCode);
            }
        }

        private void DeleteSelectedExpensePeriod()
        {
            var listView = View.FindViewById<ListView>(Resource.Id.ExpensePeriodsListView);
            var adapter = listView.Adapter as ExpensePeriodsAdapter;

            if (listView.CheckedItemCount > 0 && adapter != null)
            {
                ExpensePeriod selectedExpensePeriod = adapter[listView.CheckedItemPosition];

                var alert = new AlertDialog.Builder(Context).Create();
                alert.SetMessage(string.Format(GetString(Resource.String.DeleteExpensePeriodConfirmation), selectedExpensePeriod.StartDate.ToString("D")));
                alert.SetButton(
                    GetString(Resource.String.DeleteExpensePeriodCommand),
                    async (sender, e) => { await ExecuteDeleteExpensePeriodAsync(selectedExpensePeriod); });
                alert.SetButton2(
                    GetString(Android.Resource.String.Cancel),
                    (sender, e) => { alert.Cancel(); });

                alert.Show();
            }
        }

        private async Task ExecuteDeleteExpensePeriodAsync(ExpensePeriod expensePeriod)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            var progressDialog = new ProgressDialog(Context);
            progressDialog.Indeterminate = true;
            progressDialog.SetTitle(Resource.String.DeletingExpensePeriod);
            progressDialog.Show();

            await _persistedDataFragment.DeleteExpensePeriodAsync(Context, expensePeriod, localDestroyCancellationSource.Token);

            _persistedDataFragment.InvalidateExpensePeriods();

            if (localDestroyCancellationSource.IsCancellationRequested)
                return;

            progressDialog.Hide();

            await InitializeExpensePeriodsAsync(View);
        }

        private class ActionModeCallback : Java.Lang.Object, ActionMode.ICallback
        {
            ScheduleFragment _scheduleFragment;

            public ActionModeCallback(ScheduleFragment scheduleFragment)
            {
                _scheduleFragment = scheduleFragment;
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                if (item.ItemId == Resource.Id.EditExpensePeriodMenuItem)
                {
                    _scheduleFragment.EditSelectedExpensePeriod();

                    return true;
                }
                if (item.ItemId == Resource.Id.DeleteExpensePeriodMenuItem)
                {
                    _scheduleFragment.DeleteSelectedExpensePeriod();

                    return true;
                }
                return false;
            }

            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.ExpensePeriodMenu, menu);
                return true;
            }

            public void OnDestroyActionMode(ActionMode mode)
            {
                _scheduleFragment._actionMode = null;

                var listView = _scheduleFragment.View.FindViewById<ListView>(Resource.Id.ExpensePeriodsListView);

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