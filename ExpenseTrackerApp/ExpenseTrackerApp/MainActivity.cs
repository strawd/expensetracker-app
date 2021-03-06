﻿// Copyright 2016 David Straw

using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;

namespace ExpenseTrackerApp
{
    [Activity(Label = "@string/ApplicationName", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Android.Support.V4.App.FragmentActivity
    {
        public const string PersistedDataFragmentTag = "PersistedDataFragment";

        const string SelectedTabIndexKey = "SelectedTabIndex";

        PersistedDataFragment _persistedDataFragment;
        CancellationTokenSource _destroyCancellationSource;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            _destroyCancellationSource?.Dispose();
            _destroyCancellationSource = new CancellationTokenSource();

            var localDestroyCancellationSource = _destroyCancellationSource;

            base.OnCreate(savedInstanceState);

            _persistedDataFragment = (PersistedDataFragment)SupportFragmentManager.FindFragmentByTag(PersistedDataFragmentTag);

            if (_persistedDataFragment == null)
            {
                _persistedDataFragment = new PersistedDataFragment(ApplicationContext);

                SupportFragmentManager
                    .BeginTransaction()
                    .Add(_persistedDataFragment, PersistedDataFragmentTag)
                    .Commit();
            }
            else
            {
                _persistedDataFragment.ApplicationContext = ApplicationContext;
            }

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            var progressBar = FindViewById<ProgressBar>(Resource.Id.MainProgressBar);
            var progressText = FindViewById<TextView>(Resource.Id.MainProgressText);

            Account account;

            try
            {
                progressText.Text = GetString(Resource.String.RetrievingUserProfile);

                await _persistedDataFragment.GetOrCreateUserProfileAsync(this, localDestroyCancellationSource.Token);

                if (localDestroyCancellationSource.IsCancellationRequested)
                    return;

                progressText.Text = GetString(Resource.String.RetrievingAccountInformation);

                account = await _persistedDataFragment.GetAccountAsync(this, localDestroyCancellationSource.Token);
            }
            catch (Exception ex)
            {
                if (!localDestroyCancellationSource.IsCancellationRequested)
                {
                    var alert = new AlertDialog.Builder(this).Create();
                    alert.SetMessage(ex.Message);

                    alert.DismissEvent += (sender, e) =>
                    {
                        Finish();
                    };

                    alert.Show();
                }

                return;
            }

            if (localDestroyCancellationSource.IsCancellationRequested)
                return;

            if (account?.Id == null)
            {
                var intent = new Intent(this, typeof(NoAccountActivity));
                StartActivity(intent);
                Finish();
                return;
            }

            progressBar.Visibility = ViewStates.Gone;
            progressText.Visibility = ViewStates.Gone;

            Title = account.Name;

            InitializeViewPager();
            InitializeTabs(savedInstanceState);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_persistedDataFragment != null)
                _persistedDataFragment.ApplicationContext = null;

            _destroyCancellationSource?.Cancel();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutInt(SelectedTabIndexKey, ActionBar.SelectedNavigationIndex);

            base.OnSaveInstanceState(outState);
        }

        private void InitializeViewPager()
        {
            var viewPager = FindViewById<ViewPager>(Resource.Id.MainViewPager);

            viewPager.Adapter = new MainPagerAdapter(SupportFragmentManager);

            viewPager.PageSelected += OnViewPagerPageSelected;
        }

        private void InitializeTabs(Bundle savedInstanceState)
        {
            var summaryTab = ActionBar.NewTab()
                .SetText(Resource.String.Summary);

            var expensesTab = ActionBar.NewTab()
                .SetText(Resource.String.Expenses);

            var scheduleTab = ActionBar.NewTab()
                .SetText(Resource.String.Schedule);

            summaryTab.TabSelected += OnSummaryTabSelected;
            expensesTab.TabSelected += OnExpensesTabSelected;
            scheduleTab.TabSelected += OnScheduleTabSelected;

            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            ActionBar.AddTab(summaryTab);
            ActionBar.AddTab(expensesTab);
            ActionBar.AddTab(scheduleTab);

            int selectedTabIndex = Math.Max(0, Math.Min(ActionBar.TabCount - 1, savedInstanceState?.GetInt(SelectedTabIndexKey) ?? 0));

            ActionBar.SelectTab(ActionBar.GetTabAt(selectedTabIndex));
        }

        private void OnViewPagerPageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            var viewPager = FindViewById<ViewPager>(Resource.Id.MainViewPager);
            var adapter = (MainPagerAdapter)viewPager.Adapter;

            adapter.FinishActionMode();

            ActionBar.SetSelectedNavigationItem(e.Position);

            adapter.OnPageSelected(e.Position);
        }

        private void OnSummaryTabSelected(object sender, ActionBar.TabEventArgs e)
        {
            var viewPager = FindViewById<ViewPager>(Resource.Id.MainViewPager);

            viewPager.CurrentItem = 0;
        }

        private void OnExpensesTabSelected(object sender, ActionBar.TabEventArgs e)
        {
            var viewPager = FindViewById<ViewPager>(Resource.Id.MainViewPager);

            viewPager.CurrentItem = 1;
        }

        private void OnScheduleTabSelected(object sender, ActionBar.TabEventArgs e)
        {
            var viewPager = FindViewById<ViewPager>(Resource.Id.MainViewPager);

            viewPager.CurrentItem = 2;
        }
    }
}

