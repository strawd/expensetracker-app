// Copyright 2016 David Straw

using System;

namespace ExpenseTrackerApp.DataObjects
{
    class ExpensePeriod
    {
        public ExpensePeriod()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; set; }

        public decimal AmountAvailable { get; set; }

        public DateTimeOffset StartDate { get; set; }

        public string AccountId { get; set; }
    }
}