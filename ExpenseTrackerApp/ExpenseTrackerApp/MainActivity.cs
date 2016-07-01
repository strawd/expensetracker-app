// Copyright 2016 David Straw

using System;
using Android.App;
using Android.OS;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;

namespace ExpenseTrackerApp
{
    [Activity(Label = "ExpenseTrackerApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        MobileServiceClient client;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            client = new MobileServiceClient("https://expensetracker.azurewebsites.net");

            await client.LoginAsync(this, MobileServiceAuthenticationProvider.MicrosoftAccount);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += async (sender, args) =>
            {
                try
                {
                    button.Text = "Getting user profiles...";

                    IMobileServiceTable<UserProfile> userProfileTable = client.GetTable<UserProfile>();

                    var profiles = await userProfileTable.ToListAsync();

                    button.Text = "Profiles found: " + profiles.Count;

                    if (profiles.Count == 0)
                    {
                        var profile = new JObject();
                        profile.Add("id", Guid.NewGuid().ToString());
                        await userProfileTable.InsertAsync(new UserProfile());

                        button.Text = "User profile created.";
                    }
                }
                catch (Exception ex)
                {
                    button.Text = ex.GetType().Name + ": " + ex.Message;
                }
            };
        }
    }
}

