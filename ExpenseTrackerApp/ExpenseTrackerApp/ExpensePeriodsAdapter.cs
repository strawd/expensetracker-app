// Copyright 2016 David Straw

using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;

namespace ExpenseTrackerApp
{
    class ExpensePeriodsAdapter : BaseAdapter<ExpensePeriod>
    {
        readonly Activity _context;
        readonly List<ExpensePeriod> _expensePeriods;

        public ExpensePeriodsAdapter(Activity context, List<ExpensePeriod> items)
        {
            _context = context;
            _expensePeriods = items;
        }

        public override ExpensePeriod this[int position] => _expensePeriods[position];

        public override int Count => _expensePeriods.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null)
                view = _context.LayoutInflater.Inflate(Resource.Layout.ExpensePeriodListItem, null);

            var amountAvailableText = view.FindViewById<TextView>(Resource.Id.ExpensePeriodAmountAvailableText);
            var startDateText = view.FindViewById<TextView>(Resource.Id.ExpensePeriodStartDateText);

            amountAvailableText.Text = _expensePeriods[position].AmountAvailable.ToString("c");
            startDateText.Text = _expensePeriods[position].StartDate.ToString("d");

            return view;
        }
    }
}