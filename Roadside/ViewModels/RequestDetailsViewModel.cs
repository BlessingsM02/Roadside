using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System;
using System.Threading.Tasks;

namespace Roadside.ViewModels
{
    public class RequestDetailsViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;
        private bool _stopUpdating;

        public event EventHandler<(Location location, string mobileNumber)> UserLocationUpdated;
        public event EventHandler<(Location location, string mobileNumber)> ServiceProviderLocationUpdated;

        public RequestDetailsViewModel(string requestId)
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            PeriodicallyUpdateLocations(requestId);
        }

        private async Task PeriodicallyUpdateLocations(string requestId)
        {
            try
            {
                while (!_stopUpdating)
                {
                    await UpdateLocationsAsync(requestId);

                    // Wait for 3 seconds before the next update
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred while updating locations: " + ex.Message, "OK");
            }
        }

        private async Task UpdateLocationsAsync(string requestId)
        {
            try
            {
                var locationData = await _firebaseClient
                    .Child("tempLocations")
                    .Child(requestId)
                    .OnceSingleAsync<dynamic>();

                if (locationData != null)
                {
                    if (locationData.user != null)
                    {
                        string userMobileNumber = locationData.user.mobileNumber ?? string.Empty;
                        double userLatitude = locationData.user.location?.latitude ?? 0;
                        double userLongitude = locationData.user.location?.longitude ?? 0;

                        var userLocation = new Location(userLatitude, userLongitude);
                        UserLocationUpdated?.Invoke(this, (userLocation, userMobileNumber));
                    }

                    if (locationData.serviceProvider != null)
                    {
                        string providerMobileNumber = locationData.serviceProvider.mobileNumber ?? string.Empty;
                        double providerLatitude = locationData.serviceProvider.location?.latitude ?? 0;
                        double providerLongitude = locationData.serviceProvider.location?.longitude ?? 0;

                        var providerLocation = new Location(providerLatitude, providerLongitude);
                        ServiceProviderLocationUpdated?.Invoke(this, (providerLocation, providerMobileNumber));
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred while fetching locations: " + ex.Message, "OK");
            }
        }


        // Method to stop updating locations when it's no longer needed
        public void StopUpdating()
        {
            _stopUpdating = true;
        }
    }
}
