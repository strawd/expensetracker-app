// Copyright 2016 David Straw

using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;
using Microsoft.WindowsAzure.MobileServices;

namespace ExpenseTrackerApp
{
    public class ExpensesFragment : Fragment
    {
        private MobileServiceClient _client;

        public ExpensesFragment(MobileServiceClient client)
        {
            _client = client;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.Expenses, container, false);
        }

        public override async void OnStart()
        {
            base.OnStart();

            var listView = View.FindViewById<ListView>(Resource.Id.ExpensesListView);

            IEnumerable<ExpenseItem> expenseItems;

            try
            {
                var expenseItemTable = _client.GetTable<ExpenseItem>();
                expenseItems = await expenseItemTable.ToListAsync();
            }
            catch (Exception ex)
            {
                var alert = new AlertDialog.Builder(Context).Create();
                alert.SetMessage(ex.Message);

                alert.Show();
                return;
            }

            var expenseItemStrings = expenseItems.Select(item => $"{item.Amount}: {item.Description}");
            listView.Adapter = new ArrayAdapter<string>(Context, Android.Resource.Layout.SimpleListItem1, expenseItemStrings.ToArray());
        }
    }
}