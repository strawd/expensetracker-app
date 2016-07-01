// Copyright 2016 David Straw

using System;

namespace ExpenseTrackerApp.DataObjects
{
    public class UserProfile
    {
        public UserProfile()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; set; }

        public string UserId { get; set; }
    }
}