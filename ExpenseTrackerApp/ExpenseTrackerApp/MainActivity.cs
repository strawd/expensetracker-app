// Copyright 2016 David Straw

using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using ExpenseTrackerApp.DataObjects;
using Microsoft.WindowsAzure.MobileServices;

namespace ExpenseTrackerApp
{
    [Activity(Label = "@string/ApplicationName", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private const string CurrentUserIdKey = "CurrentUserId";
        private const string CurrentUserAuthTokenKey = "CurrentUserAuthToken";
        private const string AccountIdKey = "AccountId";
        private const string AccountNameKey = "AccountName";
        private const string UserProfileIdKey = "UserProfileId";

        MobileServiceClient _client;
        Account _account;
        UserProfile _userProfile;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            var progressBar = FindViewById<ProgressBar>(Resource.Id.MainProgressBar);
            var progressText = FindViewById<TextView>(Resource.Id.MainProgressText);

            _client = new MobileServiceClient("https://expensetracker.azurewebsites.net");

            await AuthenticateAsync(savedInstanceState);

            progressText.Text = GetString(Resource.String.RetrievingUserProfile);

            await InitializeUserProfileAsync(savedInstanceState);

            progressText.Text = GetString(Resource.String.RetrievingAccountInformation);

            await InitializeAccountAsync(savedInstanceState);

            if (_account?.Id == null)
            {
                var intent = new Intent(this, typeof(NoAccountActivity));
                StartActivity(intent);
                Finish();
                return;
            }

            progressBar.Visibility = ViewStates.Gone;
            progressText.Visibility = ViewStates.Gone;

            Title = _account.Name;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            if (_client?.CurrentUser != null)
            {
                outState.PutString(CurrentUserIdKey, _client.CurrentUser.UserId);
                outState.PutString(CurrentUserAuthTokenKey, _client.CurrentUser.MobileServiceAuthenticationToken);
            }

            if (_account != null)
            {
                outState.PutString(AccountIdKey, _account.Id);
                outState.PutString(AccountNameKey, _account.Name);
            }

            if (_userProfile != null)
            {
                outState.PutString(UserProfileIdKey, _userProfile.Id);
            }

            base.OnSaveInstanceState(outState);
        }

        private async Task AuthenticateAsync(Bundle savedInstanceState)
        {
            string currentUserId = savedInstanceState?.GetString(CurrentUserIdKey);
            string currentUserAuthToken = savedInstanceState?.GetString(CurrentUserAuthTokenKey);

            if (!string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(currentUserAuthToken))
            {
                var currentUser = new MobileServiceUser(currentUserId);
                currentUser.MobileServiceAuthenticationToken = currentUserAuthToken;
                _client.CurrentUser = currentUser;
            }
            else
            {
                await _client.LoginAsync(this, MobileServiceAuthenticationProvider.MicrosoftAccount);
            }
        }

        private async Task InitializeAccountAsync(Bundle savedInstanceState)
        {
            string accountId = savedInstanceState?.GetString(AccountIdKey);
            string accountName = savedInstanceState?.GetString(AccountNameKey);

            if (!string.IsNullOrEmpty(accountId) && !string.IsNullOrEmpty(accountName))
            {
                _account = new Account { Id = accountId, Name = accountName };
            }
            else
            {
                var accountTable = _client.GetTable<Account>();
                _account = (await accountTable.ToListAsync())?.FirstOrDefault();
            }
        }

        private async Task InitializeUserProfileAsync(Bundle savedInstanceState)
        {
            string userProfileId = savedInstanceState?.GetString(UserProfileIdKey);
            // TODO: Other settings here

            if (!string.IsNullOrEmpty(userProfileId))
            {
                _userProfile = new UserProfile { Id = userProfileId };
            }
            else
            {
                var userProfileTable = _client.GetTable<UserProfile>();
                _userProfile = (await userProfileTable.ToListAsync())?.FirstOrDefault();

                if (_userProfile == null)
                {
                    _userProfile = new UserProfile();
                    await userProfileTable.InsertAsync(_userProfile);
                }
            }
        }
    }
}

