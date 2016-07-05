// Copyright 2016 David Straw

using System;

namespace ExpenseTrackerApp.DataObjects
{
    class ExpenseItem
    {
        public ExpenseItem()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; }

        public DateTimeOffset Date { get; set; }

        public string CreatedBy { get; set; }

        public string AccountId { get; set; }
    }
}