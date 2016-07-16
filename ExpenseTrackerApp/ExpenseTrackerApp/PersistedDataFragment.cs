// Copyright 2016 David Straw

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using ExpenseTrackerApp.DataObjects;
using Microsoft.WindowsAzure.MobileServices;

namespace ExpenseTrackerApp
{
    class PersistedDataFragment : Fragment
    {
        Task<UserProfile> _getOrCreateUserProfileTask;
        Task<Account> _getAccountTask;
        Task<List<ExpenseItem>> _getExpenseItemsTask;
        Task<List<ExpensePeriod>> _getExpensePeriodsTask;
        Task<CurrentExpensePeriodSummary> _getCurrentExpensePeriodSummaryTask;

        MobileServiceClient _client;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RetainInstance = true;
        }

        public Task<UserProfile> GetOrCreateUserProfileAsync(Context context, CancellationToken cancellationToken)
        {
            return _getOrCreateUserProfileTask ??
                (_getOrCreateUserProfileTask = ExecuteWithAuthorizationAsync(context, () => ExecuteGetOrCreateUserProfileAsync(), cancellationToken));
        }

        public Task<Account> GetAccountAsync(Context context, CancellationToken cancellationToken)
        {
            return _getAccountTask ??
                (_getAccountTask = ExecuteWithAuthorizationAsync(context, () => ExecuteGetAccountAsync(), cancellationToken));
        }

        public Task<List<ExpenseItem>> GetExpenseItemsAsync(Context context, CancellationToken cancellationToken)
        {
            return _getExpenseItemsTask ?? 
                (_getExpenseItemsTask = ExecuteWithAuthorizationAsync(context, () => ExecuteGetExpenseItemsAsync(), cancellationToken));
        }

        public Task<List<ExpensePeriod>> GetExpensePeriodsAsync(Context context, CancellationToken cancellationToken)
        {
            return _getExpensePeriodsTask ??
                (_getExpensePeriodsTask = ExecuteWithAuthorizationAsync(context, () => ExecuteGetExpensePeriodsAsync(), cancellationToken));
        }

        public Task<CurrentExpensePeriodSummary> GetCurrentExpensePeriodSummaryAsync(Context context, CancellationToken cancellationToken)
        {
            return _getCurrentExpensePeriodSummaryTask ??
                (_getCurrentExpensePeriodSummaryTask = ExecuteWithAuthorizationAsync(context, () => ExecuteGetCurrentExpensePeriodSummaryAsync(), cancellationToken));
        }

        public Task InsertExpenseItemAsync(Context context, ExpenseItem expenseItem, CancellationToken cancellationToken)
        {
            return ExecuteWithAuthorizationAsync(context, () =>
            {
                var expenseItemTable = _client.GetTable<ExpenseItem>();
                return expenseItemTable.InsertAsync(expenseItem);
            }, cancellationToken);
        }

        public Task InsertExpensePeriodAsync(Context context, ExpensePeriod expensePeriod, CancellationToken cancellationToken)
        {
            return ExecuteWithAuthorizationAsync(context, () =>
            {
                var expensePeriodTable = _client.GetTable<ExpensePeriod>();
                return expensePeriodTable.InsertAsync(expensePeriod);
            }, cancellationToken);
        }

        public Task UpdateExpenseItemAsync(Context context, ExpenseItem expenseItem, CancellationToken cancellationToken)
        {
            return ExecuteWithAuthorizationAsync(context, () =>
            {
                var expenseItemTable = _client.GetTable<ExpenseItem>();
                return expenseItemTable.UpdateAsync(expenseItem);
            }, cancellationToken);
        }

        public Task UpdateExpensePeriodAsync(Context context, ExpensePeriod expensePeriod, CancellationToken cancellationToken)
        {
            return ExecuteWithAuthorizationAsync(context, () =>
            {
                var expensePeriodTable = _client.GetTable<ExpensePeriod>();
                return expensePeriodTable.UpdateAsync(expensePeriod);
            }, cancellationToken);
        }

        public Task DeleteExpenseItemAsync(Context context, ExpenseItem expenseItem, CancellationToken cancellationToken)
        {
            return ExecuteWithAuthorizationAsync(context, () =>
            {
                var expenseItemTable = _client.GetTable<ExpenseItem>();
                return expenseItemTable.DeleteAsync(expenseItem);
            }, cancellationToken);
        }

        public Task DeleteExpensePeriodAsync(Context context, ExpensePeriod expensePeriod, CancellationToken cancellationToken)
        {
            return ExecuteWithAuthorizationAsync(context, () =>
            {
                var expensePeriodTable = _client.GetTable<ExpensePeriod>();
                return expensePeriodTable.DeleteAsync(expensePeriod);
            }, cancellationToken);
        }

        public void InvalidateExpenseItems()
        {
            _getExpenseItemsTask = null;
            InvalidateSummary();
        }

        public void InvalidateExpensePeriods()
        {
            _getExpensePeriodsTask = null;
            InvalidateSummary();
        }

        public void InvalidateSummary()
        {
            _getCurrentExpensePeriodSummaryTask = null;
        }

        private async Task<T> ExecuteWithAuthorizationAsync<T>(Context context, Func<Task<T>> action, CancellationToken cancellationToken)
        {
            if (_client == null)
            {
                _client = new MobileServiceClient("https://expensetracker.azurewebsites.net");
                await _client.LoginAsync(context, MobileServiceAuthenticationProvider.MicrosoftAccount);
            }

            try
            {
                return await action();
            }
            catch (MobileServiceInvalidOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await _client.LoginAsync(context, MobileServiceAuthenticationProvider.MicrosoftAccount);

                    return await action();
                }

                throw;
            }
        }

        private Task ExecuteWithAuthorizationAsync(Context context, Func<Task> action, CancellationToken cancellationToken)
        {
            return ExecuteWithAuthorizationAsync(context, async () => { await action(); return 0; }, cancellationToken);
        }

        private async Task<UserProfile> ExecuteGetOrCreateUserProfileAsync()
        {
            var userProfileTable = _client.GetTable<UserProfile>();
            var userProfile = (await userProfileTable.ToListAsync())?.FirstOrDefault();

            if (userProfile == null)
            {
                userProfile = new UserProfile();
                await userProfileTable.InsertAsync(userProfile);
            }

            return userProfile;
        }

        private async Task<Account> ExecuteGetAccountAsync()
        {
            var accountTable = _client.GetTable<Account>();
            return (await accountTable.ToListAsync())?.FirstOrDefault();
        }

        private Task<List<ExpenseItem>> ExecuteGetExpenseItemsAsync()
        {
            var expenseItemTable = _client.GetTable<ExpenseItem>();
            return expenseItemTable.CreateQuery()
                .OrderByDescending(x => x.Date)
                .Take(100)
                .ToListAsync();
        }

        private Task<List<ExpensePeriod>> ExecuteGetExpensePeriodsAsync()
        {
            var expensePeriodTable = _client.GetTable<ExpensePeriod>();
            return expensePeriodTable.CreateQuery()
                .OrderByDescending(x => x.StartDate)
                .Take(100)
                .ToListAsync();
        }

        private Task<CurrentExpensePeriodSummary> ExecuteGetCurrentExpensePeriodSummaryAsync()
        {
            return _client.InvokeApiAsync<CurrentExpensePeriodSummary>("Summary/CurrentExpensePeriod", HttpMethod.Get, null);
        }
    }
}