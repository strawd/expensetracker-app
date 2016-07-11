// Copyright 2016 David Straw

using System.Collections.Generic;
using System.Linq;
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
        Task _authenticateTask;
        Task<UserProfile> _getOrCreateUserProfileTask;
        Task<Account> _getAccountTask;
        Task<List<ExpenseItem>> _getExpenseItemsTask;
        Task<List<ExpensePeriod>> _getExpensePeriodsTask;

        MobileServiceClient _client;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RetainInstance = true;
        }

        public Task AuthenticateAsync(Context context)
        {
            if (_client == null)
                _client = new MobileServiceClient("https://expensetracker.azurewebsites.net");

            return _authenticateTask ??
                (_authenticateTask = _client.LoginAsync(context, MobileServiceAuthenticationProvider.MicrosoftAccount));
        }

        public Task<UserProfile> GetOrCreateUserProfileAsync()
        {
            return _getOrCreateUserProfileTask ??
                (_getOrCreateUserProfileTask = ExecuteGetOrCreateUserProfileAsync());
        }

        public Task<Account> GetAccountAsync()
        {
            return _getAccountTask ??
                (_getAccountTask = ExecuteGetAccountAsync());
        }

        public Task<List<ExpenseItem>> GetExpenseItemsAsync()
        {
            return _getExpenseItemsTask ?? 
                (_getExpenseItemsTask = ExecuteGetExpenseItemsAsync());
        }

        public Task<List<ExpensePeriod>> GetExpensePeriodsAsync()
        {
            return _getExpensePeriodsTask ??
                (_getExpensePeriodsTask = ExecuteGetExpensePeriodsAsync());
        }

        public Task InsertExpenseItemAsync(ExpenseItem expenseItem)
        {
            var expenseItemTable = _client.GetTable<ExpenseItem>();
            return expenseItemTable.InsertAsync(expenseItem);
        }

        public Task InsertExpensePeriodAsync(ExpensePeriod expensePeriod)
        {
            var expensePeriodTable = _client.GetTable<ExpensePeriod>();
            return expensePeriodTable.InsertAsync(expensePeriod);
        }

        public void InvalidateExpenseItems()
        {
            _getExpenseItemsTask = null;
        }

        public void InvalidateExpensePeriods()
        {
            _getExpensePeriodsTask = null;
        }

        public Task UpdateExpenseItemAsync(ExpenseItem expenseItem)
        {
            var expenseItemTable = _client.GetTable<ExpenseItem>();
            return expenseItemTable.UpdateAsync(expenseItem);
        }

        public Task UpdateExpensePeriodAsync(ExpensePeriod expensePeriod)
        {
            var expensePeriodTable = _client.GetTable<ExpensePeriod>();
            return expensePeriodTable.UpdateAsync(expensePeriod);
        }

        public Task DeleteExpenseItemAsync(ExpenseItem expenseItem)
        {
            var expenseItemTable = _client.GetTable<ExpenseItem>();
            return expenseItemTable.DeleteAsync(expenseItem);
        }

        public Task DeleteExpensePeriodAsync(ExpensePeriod expensePeriod)
        {
            var expensePeriodTable = _client.GetTable<ExpensePeriod>();
            return expensePeriodTable.DeleteAsync(expensePeriod);
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
    }
}