using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using System.Linq;
using Windows.Security.Credentials;
using Newtonsoft.Json.Linq;

using Windows.Networking.PushNotifications;
using System.Net.Http;

// To add offline sync support, add the NuGet package Microsoft.WindowsAzure.MobileServices.SQLiteStore
// to your project. Then, uncomment the lines marked // offline sync
// For more information, see: http://go.microsoft.com/fwlink/?LinkId=717898
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;  
using Microsoft.WindowsAzure.MobileServices.Sync;         

namespace todolist_complete
{
    public sealed partial class MainPage : Page
    {
        private MobileServiceCollection<TodoItem, TodoItem> items;
        //private IMobileServiceTable<TodoItem> todoTable = App.MobileService.GetTable<TodoItem>();

        // We are using an offline sync table implemented by SQLite.
        private IMobileServiceSyncTable<TodoItem> todoTable = App.MobileService.GetSyncTable<TodoItem>(); 

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //await InitLocalStoreAsync(); // offline sync
            // ButtonRefresh_Click(this, null);
        }

        // Push authentication code added in http://aka.ms/m79ei6
        #region push notifications
        // Registers for template push notifications.
        private async void InitNotificationsAsync()
        {
            var channel = await PushNotificationChannelManager
                .CreatePushNotificationChannelForApplicationAsync();

            // Define a toast templates for WNS.
            var toastTemplate =
                @"<toast><visual><binding template=""ToastText02""><text id=""1"">"
                                + @"New item:</text><text id=""2"">"
                                + @"$(messageParam)</text></binding></visual></toast>";

            JObject templateBody = new JObject();
            templateBody["body"] = toastTemplate;

            // Add the required WNS toast header.
            JObject wnsToastHeaders = new JObject();
            wnsToastHeaders["X-WNS-Type"] = "wns/toast";
            templateBody["headers"] = wnsToastHeaders;

            JObject templates = new JObject();
            templates["testTemplate"] = templateBody;

            try
            {
                // Register for template push notifications.
                await App.MobileService.GetPush()
                .RegisterAsync(channel.Uri, templates);

                // Define two new tags as a JSON array.
                var body = new JArray();
                body.Add("broadcast");
                body.Add("test");

                // Call the custom API '/api/updatetags/<installationid>' 
                // with the JArray of tags.
                var response = await App.MobileService
                    .InvokeApiAsync("updatetags/"
                    + App.MobileService.InstallationId, body);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Push registration failed");
            }
        }
        #endregion
        #region authentication
        private async void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            // Login the user and then load data from the mobile app.
            if (await AuthenticateAsync())
            {
                // Register for push notifications.
                InitNotificationsAsync();

                // Switch the buttons and load items from the mobile app.
                ButtonLogin.Visibility = Visibility.Collapsed;
                ButtonSave.Visibility = Visibility.Visible;

                await InitLocalStoreAsync(); //offline sync support.
                await RefreshTodoItems();
            }
        }

        // Define a member variable for storing the signed-in user. 
        private MobileServiceUser user;

        // Define a method that performs the authentication process
        // using a Facebook sign-in and caches authentication tokens. 
        private async System.Threading.Tasks.Task<bool> AuthenticateAsync()
        {
            string message;
            bool success = false;

            // This sample uses the Microsoft Account provider.
            var provider = MobileServiceAuthenticationProvider.MicrosoftAccount;

            // Use the PasswordVault to securely store and access credentials.
            PasswordVault vault = new PasswordVault();
            PasswordCredential credential = null;

            try
            {
                // Try to get an existing credential from the vault.
                credential = vault.FindAllByResource(provider.ToString()).FirstOrDefault();
            }
            catch (Exception)
            {
                // When there is no matching resource an error occurs, which we ignore.
            }

            // If we have a valid and non-expired token, use it;
            // otherwise, sign-in again.
            if (credential != null && !App.MobileService.IsTokenExpired(credential))
            {
                // Create a user from the stored credentials.
                user = new MobileServiceUser(credential.UserName);
                credential.RetrievePassword();
                user.MobileServiceAuthenticationToken = credential.Password;

                // Set the user from the stored credentials.
                App.MobileService.CurrentUser = user;

                // Notify the user that cached credentials were used.
                message = string.Format("Signed-in with cached credentials for user - {0}", user.UserId);
                success = true;
            }
            else
            {
                try
                {
                    // If we have an expired token, remove it.
                    if (credential != null)
                    {
                        vault.Remove(credential);
                    }

                    // Login with the identity provider.
                    user = await App.MobileService
                        .LoginAsync(provider);

                    // Create and store the user credentials.
                    credential = new PasswordCredential(provider.ToString(),
                        user.UserId, user.MobileServiceAuthenticationToken);
                    vault.Add(credential);

                    // Welcome user and display login SID info.
                    message = string.Format("You are now logged in - {0}", user.UserId);
                    success = true;
                }

                catch (InvalidOperationException)
                {
                    message = "You must log in. Login Required";
                }
            }

            await new MessageDialog(message).ShowAsync();

            return success;
        }
        //private async System.Threading.Tasks.Task<bool> AuthenticateAsync()
        //{
        //    string message;
        //    bool success = false;
        //    try
        //    {
        //        // Change 'MobileService' to the name of your MobileServiceClient instance.
        //        // Sign-in using Facebook authentication.
        //        user = await App.MobileService
        //            .LoginAsync(MobileServiceAuthenticationProvider.Facebook);
        //        message =
        //            string.Format("You are now signed in - {0}", user.UserId);

        //        success = true;
        //    }
        //    catch (InvalidOperationException)
        //    {
        //        message = "You must log in. Login Required";
        //    }

        //    var dialog = new MessageDialog(message);
        //    dialog.Commands.Add(new UICommand("OK"));
        //    await dialog.ShowAsync();
        //    return success;
        //}
        #endregion
        private async Task InsertTodoItem(TodoItem todoItem)
        {
            // This code inserts a new TodoItem into the database. When the operation completes
            // and Mobile Apps has assigned an Id, the item is added to the CollectionView
            await todoTable.InsertAsync(todoItem);
            items.Add(todoItem);

            // Upload offline changes to the backend.
            await App.MobileService.SyncContext.PushAsync();             
        }

        private async Task RefreshTodoItems()
        {
            MobileServiceInvalidOperationException exception = null;
            try
            {
                // This code refreshes the entries in the list view by querying the TodoItems table.
                // The query excludes completed TodoItems
                items = await todoTable
                    .Where(todoItem => todoItem.Complete == false)
                    .ToCollectionAsync();
            }
            catch (MobileServiceInvalidOperationException e)
            {
                exception = e;
            }

            if (exception != null)
            {
                await new MessageDialog(exception.Message, "Error loading items").ShowAsync();
            }
            else
            {
                ListItems.ItemsSource = items;
                this.ButtonSave.IsEnabled = true;
            }
        }

        private async Task UpdateCheckedTodoItem(TodoItem item)
        {
            // This code takes a freshly completed TodoItem and updates the database. When the MobileService
            // responds, the item is removed from the list
            await todoTable.UpdateAsync(item);
            items.Remove(item);
            ListItems.Focus(Windows.UI.Xaml.FocusState.Unfocused);

            // Upload offline changes to the backend.
            await App.MobileService.SyncContext.PushAsync();
        }

        private async void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            ButtonRefresh.IsEnabled = false;

            await SyncAsync(); 
            await RefreshTodoItems();

            ButtonRefresh.IsEnabled = true;
        }

        private async void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var todoItem = new TodoItem { Text = TextInput.Text };
            TextInput.Text = "";
            await InsertTodoItem(todoItem);
        }

        private async void CheckBoxComplete_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            TodoItem item = cb.DataContext as TodoItem;
            await UpdateCheckedTodoItem(item);
        }

        private void TextInput_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ButtonSave.Focus(FocusState.Programmatic);
            }
        }

        #region Offline sync

        private async Task InitLocalStoreAsync()
        {
            if (!App.MobileService.SyncContext.IsInitialized)
            {
                var store = new MobileServiceSQLiteStore("localstore.db");
                store.DefineTable<TodoItem>();
                await App.MobileService.SyncContext.InitializeAsync(store);
            }

            await SyncAsync();
        }

        private async Task SyncAsync()
        {
            await App.MobileService.SyncContext.PushAsync(); 
            await todoTable.PullAsync("todoItems", todoTable.CreateQuery());
        }

        #endregion
    }
}
