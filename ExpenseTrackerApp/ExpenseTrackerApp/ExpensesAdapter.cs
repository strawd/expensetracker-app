// Copyright 2016 David Straw

using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;

namespace ExpenseTrackerApp
{
    class ExpensesAdapter : BaseAdapter<ExpenseItem>
    {
        Activity _context;
        List<ExpenseItem> _expenseItems;

        public ExpensesAdapter(Activity context, List<ExpenseItem> items)
        {
            _context = context;
            _expenseItems = items;
        }

        public override ExpenseItem this[int position] => _expenseItems[position];

        public override int Count => _expenseItems.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null)
                view = _context.LayoutInflater.Inflate(Resource.Layout.ExpenseListItem, null);

            var descriptionText = view.FindViewById<TextView>(Resource.Id.ExpenseItemDescriptionText);
            var amountText = view.FindViewById<TextView>(Resource.Id.ExpenseItemAmountCheckedText);

            descriptionText.Text = _expenseItems[position].Description;
            amountText.Text = _expenseItems[position].Amount.ToString("c");

            return view;
        }
    }
}