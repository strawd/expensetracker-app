// Copyright 2016 David Straw

using System;
using Android.Support.V4.App;

namespace ExpenseTrackerApp
{
    class MainPagerAdapter : FragmentPagerAdapter
    {
        const int SummaryPosition = 0;
        const int ExpensesPosition = 1;
        const int SchedulePosition = 2;

        ExpensesFragment _expensesFragment;
        ScheduleFragment _scheduleFragment;

        public MainPagerAdapter(FragmentManager fm)
            : base(fm)
        {
        }

        public override int Count { get; } = 3;

        public override Fragment GetItem(int position)
        {
            switch (position)
            {
                case SummaryPosition:
                    return new SummaryFragment();
                case ExpensesPosition:
                    return (_expensesFragment = new ExpensesFragment());
                case SchedulePosition:
                    return (_scheduleFragment = new ScheduleFragment());
            }

            throw new InvalidOperationException("Invalid pager position");
        }

        public void FinishActionMode()
        {
            _expensesFragment?.FinishActionMode();
            _scheduleFragment?.FinishActionMode();
        }
    }
}