// Copyright 2016 David Straw

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Util;
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

        public async Task InitializeSummaryAsync(View view)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            var expensePeriodSummaryLayout = view.FindViewById<LinearLayout>(Resource.Id.ExpensePeriodSummaryLayout);
            var progressBar = view.FindViewById<ProgressBar>(Resource.Id.SummaryProgressBar);
            var progressText = view.FindViewById<TextView>(Resource.Id.SummaryProgressText);
            var refreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.SummaryRefreshLayout);

            expensePeriodSummaryLayout.Visibility = ViewStates.Gone;
            progressBar.Visibility = ViewStates.Visible;
            progressText.Visibility = ViewStates.Visible;

            progressText.Text = GetString(Resource.String.RetrievingSummary);

            List<ExpensePeriodSummary> expensePeriodSummaries;

            try
            {
                expensePeriodSummaries = await _persistedDataFragment.GetExpensePeriodSummariesAsync(Context, localDestroyCancellationSource.Token);
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

            if (expensePeriodSummaries.Count == 0)
            {
                expensePeriodSummaries.Add(new ExpensePeriodSummary
                {
                    AmountAvailable = 0,
                    AmountRemaining = 0,
                    ExpensesCount = 0,
                    StartDate = DateTimeOffset.Now
                });
            }

            expensePeriodSummaryLayout.RemoveAllViews();

            foreach (var expensePeriodSummary in expensePeriodSummaries)
            {
                var labelText = new TextView(Context);
                var labelLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                labelLayoutParams.BottomMargin = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 15f, Resources.DisplayMetrics);
                labelText.LayoutParameters = labelLayoutParams;
                labelText.Text = string.Format(GetString(Resource.String.ExpensePeriodSummaryLabel), expensePeriodSummary.StartDate.ToString("D"));
                expensePeriodSummaryLayout.AddView(labelText);

                var amountLayout = new LinearLayout(Context);
                var amountLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 35f, Resources.DisplayMetrics));
                amountLayout.LayoutParameters = amountLayoutParams;
                amountLayout.Orientation = Orientation.Horizontal;
                expensePeriodSummaryLayout.AddView(amountLayout);

                var amountRemainingView = new View(Context);
                var amountRemainingLayoutParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent);
                amountRemainingView.LayoutParameters = amountRemainingLayoutParams;
                amountLayout.AddView(amountRemainingView);

                var amountSpentView = new View(Context);
                var amountSpentLayoutParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent);
                amountSpentView.LayoutParameters = amountSpentLayoutParams;
                amountLayout.AddView(amountSpentView);

                float amountRemaining = (float)expensePeriodSummary.AmountRemaining;
                float amountSpent = (float)(expensePeriodSummary.AmountAvailable - expensePeriodSummary.AmountRemaining);

                amountRemainingLayoutParams.Weight = amountRemaining;
                amountSpentLayoutParams.Weight = amountSpent;

                Color amountRemainingColor = Color.ParseColor("#00FF00");
                Color amountSpentColor = Color.ParseColor("#55AA55");
                if (amountSpent > 0f)
                {
                    if (amountRemaining / amountSpent < (1f / 9f))
                    {
                        amountRemainingColor = Color.ParseColor("#FF3200");
                        amountSpentColor = Color.ParseColor("#A03A23");
                    }
                    else if (amountRemaining / amountSpent < (1f / 3f))
                    {
                        amountRemainingColor = Color.ParseColor("#EDF900");
                        amountSpentColor = Color.ParseColor("#BAC132");
                    }
                }

                amountRemainingView.SetBackgroundColor(amountRemainingColor);
                amountSpentView.SetBackgroundColor(amountSpentColor);

                var expensePeriodProgressText = new TextView(Context);
                var expensePeriodProgressLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                expensePeriodProgressLayoutParams.BottomMargin = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 20f, Resources.DisplayMetrics);
                expensePeriodProgressText.LayoutParameters = expensePeriodProgressLayoutParams;
                expensePeriodProgressText.Gravity = GravityFlags.Right;
                expensePeriodProgressText.Text = string.Format(
                    GetString(Resource.String.ExpensePeriodProgressSummary),
                    expensePeriodSummary.AmountRemaining.ToString("c"),
                    expensePeriodSummary.AmountAvailable.ToString("c"));
                expensePeriodSummaryLayout.AddView(expensePeriodProgressText);
            }

            progressBar.Visibility = ViewStates.Gone;
            progressText.Visibility = ViewStates.Gone;
            expensePeriodSummaryLayout.Visibility = ViewStates.Visible;

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