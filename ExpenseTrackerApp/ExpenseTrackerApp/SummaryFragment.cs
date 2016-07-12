// Copyright 2016 David Straw

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;

namespace ExpenseTrackerApp
{
    public class SummaryFragment : Fragment
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

            var view = inflater.Inflate(Resource.Layout.Summary, container, false);

#pragma warning disable CS4014 // Intentionally fire-and-forget
            InitializeSummaryAsync(view);
#pragma warning restore CS4014

            return view;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            _destroyCancellationSource?.Cancel();
        }

        private async Task InitializeSummaryAsync(View view)
        {
            var localDestroyCancellationSource = _destroyCancellationSource;

            var expensePeriodSummaryLayout = view.FindViewById<LinearLayout>(Resource.Id.CurrentExpensePeriodSummaryLayout);
            var expensePeriodProgressBar = view.FindViewById<ProgressBar>(Resource.Id.CurrentExpensePeriodProgressBar);
            var expensePeriodProgressText = view.FindViewById<TextView>(Resource.Id.CurrentExpensePeriodProgressText);
            var expensePeriodStartDateText = view.FindViewById<TextView>(Resource.Id.CurrentExpensePeriodStartDateText);
            var expensePeriodExpenseCountText = view.FindViewById<TextView>(Resource.Id.CurrentExpensePeriodExpenseCountText);
            var progressBar = view.FindViewById<ProgressBar>(Resource.Id.SummaryProgressBar);
            var progressText = view.FindViewById<TextView>(Resource.Id.SummaryProgressText);

            expensePeriodSummaryLayout.Visibility = ViewStates.Gone;
            progressBar.Visibility = ViewStates.Visible;
            progressText.Visibility = ViewStates.Visible;

            progressText.Text = GetString(Resource.String.RetrievingSummary);

            CurrentExpensePeriodSummary currentExpensePeriodSummary;

            try
            {
                currentExpensePeriodSummary = await _persistedDataFragment.GetCurrentExpensePeriodSummaryAsync();
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
            expensePeriodSummaryLayout.Visibility = ViewStates.Visible;

            expensePeriodProgressBar.Max = (int)(currentExpensePeriodSummary.AmountAvailable * 100m);
            expensePeriodProgressBar.Progress = (int)(currentExpensePeriodSummary.AmountRemaining * 100m);
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
        }
    }
}