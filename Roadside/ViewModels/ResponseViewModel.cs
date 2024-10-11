using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using Roadside.Views;

namespace Roadside.ViewModels
{
    public class ResponseViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;
        private bool _stopChecking;

        public ResponseViewModel(string key)
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            CheckAndDeletePendingRecord(key);
            PeriodicallyCheckRequestTable(key);
        }

        

        private async Task CheckAndDeletePendingRecord(string key)
        {
            try
            {
                // Wait for 1 minute before checking the status
                await Task.Delay(TimeSpan.FromMinutes(1));

                // Retrieve the record by key
                var record = await _firebaseClient
                    .Child("ClickedMobileNumbers")
                    .Child(key)
                    .OnceSingleAsync<dynamic>();

                // Check if the status is still "Pending"
                if (record != null && record.Status == "Pending")
                {
                    // Delete the record
                    await _firebaseClient
                        .Child("ClickedMobileNumbers")
                        .Child(key)
                        .DeleteAsync();

                    await Application.Current.MainPage.DisplayAlert("Info", "Service Provider did not respond", "OK");
                    await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                await Application.Current.MainPage.DisplayAlert("Error", "Something went wrong: " + ex.Message, "OK");
            }
        }

        private async Task PeriodicallyCheckRequestTable(string key)
        {
            try
            {
                while (!_stopChecking) // Continuously check until stopped
                {
                    // Check every 30 seconds
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    // Query the "requests" table to see if the user exists
                    var request = await _firebaseClient
                        .Child("requests")
                        .Child(key)
                        .OnceSingleAsync<dynamic>();

                    if (request != null)
                    {
                        // User exists in the "requests" table
                        await Application.Current.MainPage.DisplayAlert("Success", "Your request has been accepted.", "OK");
                        _stopChecking = true; // Stop further checks

                        // Navigate to the appropriate page if needed
                        await App.Current.MainPage.Navigation.PushAsync(new RequestDetailsPage());
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred while checking the request table: " + ex.Message, "OK");
            }
        }

        // Method to stop the periodic checking when it's no longer needed
        public void StopChecking()
        {
            _stopChecking = true;
        }
    }
}