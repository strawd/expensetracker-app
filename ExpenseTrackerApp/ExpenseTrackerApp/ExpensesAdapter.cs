// Copyright 2016 David Straw

using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;

namespace ExpenseTrackerApp
{
    class ExpensesAdapter : BaseAdapter<ExpenseItem>, ISectionIndexer
    {
        readonly Activity _context;
        readonly List<ExpenseItem> _expenseItems;
        readonly string[] _sections;
        readonly Java.Lang.Object[] _sectionJavaObjects;
        readonly Dictionary<string, int> _sectionIndexMap;

        public ExpensesAdapter(Activity context, List<ExpenseItem> items)
        {
            _context = context;
            _expenseItems = items;

            _sectionIndexMap = new Dictionary<string, int>();

            for (int i = 0; i < _expenseItems.Count; i++)
            {
                var key = _expenseItems[i].Date.ToString("d");
                if (!_sectionIndexMap.ContainsKey(key))
                    _sectionIndexMap[key] = i;
            }

            _sections = _sectionIndexMap.Keys.ToArray();

            _sectionJavaObjects = _sections
                .Select(x => new Java.Lang.String(x))
                .Cast<Java.Lang.Object>()
                .ToArray();
        }

        public int GetPositionForSection(int sectionIndex)
        {
            return _sectionIndexMap[_sections[sectionIndex]];
        }

        public int GetSectionForPosition(int position)
        {
            return _sectionIndexMap.Last(pair => pair.Value < position).Value;
        }

        public Java.Lang.Object[] GetSections()
        {
            return _sectionJavaObjects;
        }

        public override ExpenseItem this[int position] => _expenseItems[position];

        public override int Count => _expenseItems.Count;

        public override long GetItemId(int position) => position;

        public override int ViewTypeCount => 2;

        public override int GetItemViewType(int position)
        {
            if (_sectionIndexMap.ContainsValue(position))
                return 0;

            return 1;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            bool hasSectionHeader = _sectionIndexMap.ContainsValue(position);

            View view = convertView;
            if (view == null)
            {
                if (hasSectionHeader)
                    view = _context.LayoutInflater.Inflate(Resource.Layout.ExpenseListItemWithSectionHeader, null);
                else
                    view = _context.LayoutInflater.Inflate(Resource.Layout.ExpenseListItem, null);
            }

            var descriptionText = view.FindViewById<TextView>(Resource.Id.ExpenseItemDescriptionText);
            var amountText = view.FindViewById<TextView>(Resource.Id.ExpenseItemAmountCheckedText);

            descriptionText.Text = _expenseItems[position].Description;
            amountText.Text = _expenseItems[position].Amount.ToString("c");

            if (hasSectionHeader)
            {
                var sectionText = view.FindViewById<TextView>(Resource.Id.ExpenseItemSectionText);
                sectionText.Text = _expenseItems[position].Date.ToString("d");
            }

            return view;
        }
    }
}