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

        public ResponseViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            PeriodicallyCheckRequestTable();
        }

        private async Task PeriodicallyCheckRequestTable()
        {
            try
            {
                var elapsedTime = TimeSpan.Zero;
                var checkInterval = TimeSpan.FromSeconds(5);
                var maxWaitTime = TimeSpan.FromMinutes(1);
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                while (!_stopChecking && elapsedTime < maxWaitTime)
                {
                    // Query all records in the "requests" table
                    var requests = await _firebaseClient
                        .Child("requests")
                        .OnceAsync<dynamic>();

                    foreach (var request in requests)
                    {
                        // Check if the user has made a request and the driver ID matches the mobile number in preferences
                        if (request.Object.DriverId == mobileNumber)
                        {
                            // The driver has accepted the request
                            
                            await Application.Current.MainPage.DisplayAlert("Success", "Your request has been accepted.", "OK");
                            _stopChecking = true; // Stop further checks

                            // Navigate to the appropriate page if needed
                            //await Navigation.PushAsync(new Views.NewPage1());
                            await Shell.Current.GoToAsync($"//{nameof(RequestDetailsPage)}");
                            //await App.Current.MainPage.Navigation.PushAsync(new RequestDetailsPage());
                            return; // Exit the method as the request is accepted
                        }
                    }

                    // Wait for the next check
                    await Task.Delay(checkInterval);
                    elapsedTime += checkInterval;
                }

                // If after the max wait time the request is still pending, delete the pending record
                if (!_stopChecking)
                {
                    await DeletePendingRecord(mobileNumber);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred while monitoring the request status: ", "OK");
            }
        }

        private async Task DeletePendingRecord(string mobileNumber)
        {
            try
            {
                // Retrieve all records in the "ClickedMobileNumbers" table
                var records = await _firebaseClient
                    .Child("ClickedMobileNumbers")
                    .OnceAsync<dynamic>();

                // Find the record with the mobile number and status "Pending"
                foreach (var record in records)
                {
                    if (record.Object.MobileNumber == mobileNumber && record.Object.Status == "Pending")
                    {
                        // Delete the pending record
                        await _firebaseClient
                            .Child("ClickedMobileNumbers")
                            .Child(record.Key)
                            .DeleteAsync();

                        await Application.Current.MainPage.DisplayAlert("Info", "Service Provider did not respond in time. Your request has been canceled.", "OK");
                        await App.Current.MainPage.Navigation.PushAsync(new LoadingPage());
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                await Application.Current.MainPage.DisplayAlert("Error", "Something went wrong while deleting the pending record: ", "OK");
            }
        }

        // Method to stop the periodic checking when it's no longer needed
        public void StopChecking()
        {
            _stopChecking = true;
        }
    }
}
