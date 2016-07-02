// Copyright 2016 David Straw

using Android.App;
using Android.OS;
using Android.Views;

namespace ExpenseTrackerApp
{
    public class ExpensesFragment : Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.Expenses, container, false);
        }
    }
}