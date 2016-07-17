// Copyright 2016 David Straw

using System;

namespace ExpenseTrackerApp.DataObjects
{
    class ExpensePeriodSummary
    {
        public decimal AmountAvailable { get; set; }

        public decimal AmountRemaining { get; set; }

        public int ExpensesCount { get; set; }

        public DateTimeOffset StartDate { get; set; }
    }
}