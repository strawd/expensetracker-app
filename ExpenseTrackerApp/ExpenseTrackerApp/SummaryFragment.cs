// Copyright 2016 David Straw

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;

namespace ExpenseTrackerApp
{
    public class SummaryFragment : Android.Support.V4.App.Fragment
    {
        PersistedDataFragment _persistedDataFragment;
        CancellationTokenSource _destroyCancellationSource;
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

            var view = inflater.Inflate(Resource.Layout.Summary, container, false);
            var refreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.SummaryRefreshLayout);

            refreshLayout.Refresh += OnRefreshLayoutRefresh;

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeSummaryAsync(view);
#pragma warning restore CS4014

            return view;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            _destroyCancellationSource?.Cancel();
            _refreshing = false;
        }

        private async Task InitializeSummaryAsync(View view)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            var expensePeriodSummaryLayout = view.FindViewById<LinearLayout>(Resource.Id.CurrentExpensePeriodSummaryLayout);
            var amountRemainingView = view.FindViewById<View>(Resource.Id.SummaryAmountRemainingView);
            var amountSpentView = view.FindViewById<View>(Resource.Id.SummaryAmountSpentView);
            var expensePeriodProgressText = view.FindViewById<TextView>(Resource.Id.CurrentExpensePeriodProgressText);
            var expensePeriodStartDateText = view.FindViewById<TextView>(Resource.Id.CurrentExpensePeriodStartDateText);
            var expensePeriodExpenseCountText = view.FindViewById<TextView>(Resource.Id.CurrentExpensePeriodExpenseCountText);
            var progressBar = view.FindViewById<ProgressBar>(Resource.Id.SummaryProgressBar);
            var progressText = view.FindViewById<TextView>(Resource.Id.SummaryProgressText);
            var refreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.SummaryRefreshLayout);

            expensePeriodSummaryLayout.Visibility = ViewStates.Gone;
            progressBar.Visibility = ViewStates.Visible;
            progressText.Visibility = ViewStates.Visible;

            progressText.Text = GetString(Resource.String.RetrievingSummary);

            CurrentExpensePeriodSummary currentExpensePeriodSummary;

            try
            {
                currentExpensePeriodSummary = await _persistedDataFragment.GetCurrentExpensePeriodSummaryAsync(Context, localDestroyCancellationSource.Token);
            }
            catch (Exception ex)
            {
                _persistedDataFragment.InvalidateSummary();

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
            expensePeriodSummaryLayout.Visibility = ViewStates.Visible;

            float amountRemaining = (float)currentExpensePeriodSummary.AmountRemaining;
            float amountSpent = (float)(currentExpensePeriodSummary.AmountAvailable - currentExpensePeriodSummary.AmountRemaining);

            ((LinearLayout.LayoutParams)amountRemainingView.LayoutParameters).Weight = amountRemaining;
            ((LinearLayout.LayoutParams)amountSpentView.LayoutParameters).Weight = amountSpent;

            Color amountRemainingColor = Color.ParseColor("#00FF00");
            Color amountSpentColor = Color.ParseColor("#55AA55");
            if (amountSpent > 0f)
            {
                if (amountRemaining / amountSpent < (1f/9f))
                {
                    amountRemainingColor = Color.ParseColor("#FF3200");
                    amountSpentColor = Color.ParseColor("#A03A23");
                }
                else if (amountRemaining / amountSpent < (1f/3f))
                {
                    amountRemainingColor = Color.ParseColor("#EDF900");
                    amountSpentColor = Color.ParseColor("#BAC132");
                }
            }

            amountRemainingView.SetBackgroundColor(amountRemainingColor);
            amountSpentView.SetBackgroundColor(amountSpentColor);

            expensePeriodProgressText.Text = string.Format(
                GetString(Resource.String.CurrentExpensePeriodProgressSummary),
                currentExpensePeriodSummary.AmountRemaining.ToString("c"),
                currentExpensePeriodSummary.AmountAvailable.ToString("c"));
            expensePeriodStartDateText.Text = string.Format(
                GetString(Resource.String.CurrentExpensePeriodStartDate),
                currentExpensePeriodSummary.StartDate.ToString("D"));
            expensePeriodExpenseCountText.Text = string.Format(
                GetString(Resource.String.CurrentExpensePeriodExpenseCount),
                currentExpensePeriodSummary.ExpensesCount);

            refreshLayout.Refreshing = false;
            _refreshing = false;
        }

        private void OnRefreshLayoutRefresh(object sender, EventArgs e)
        {
            var refreshLayout = View.FindViewById<SwipeRefreshLayout>(Resource.Id.SummaryRefreshLayout);

            if (_refreshing)
            {
                refreshLayout.Refreshing = false;
                return;
            }

            _persistedDataFragment.InvalidateSummary();

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeSummaryAsync(View);
#pragma warning restore CS4014
        }
    }
}