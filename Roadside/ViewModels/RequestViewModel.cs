using Firebase.Database;
using Firebase.Database.Query;
using Mopups.Services;
using Newtonsoft.Json;
using Roadside.Models;
using Roadside.Views;
using System.Text;

namespace Roadside.ViewModels
{
    internal class RequestViewModel : BindableObject
    {
        private string _firstName;
        private string _lastName;
        private string _vehicleDescription;
        private string _plateNumber;
        private string _serviceProviderId;
        private string _mobileNumber;
        private string _latitude;
        private string _longitude;
        private readonly FirebaseClient _firebaseClient;
        private readonly FirebaseClient _firebaseClient2;

        public RequestViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
            _firebaseClient2 = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            LoadUserProfileCommand = new Command(async () => await LoadUserDetailsAsync());
            SubmitRequestCommand = new Command(async () => await SubmitRequestAsync());
            LoadUserProfileCommand.Execute(null);
        }

        public string ServiceProviderId
        {
            get => _serviceProviderId;
            set
            {
                _serviceProviderId = value;
                OnPropertyChanged();
            }
        }

        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                OnPropertyChanged();
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                _lastName = value;
                OnPropertyChanged();
            }
        }

        public string VehicleDescription
        {
            get => _vehicleDescription;
            set
            {
                _vehicleDescription = value;
                OnPropertyChanged();
            }
        }

        public string PlateNumber
        {
            get => _plateNumber;
            set
            {
                _plateNumber = value;
                OnPropertyChanged();
            }
        }

        public string MobileNumber
        {
            get => _mobileNumber;
            set
            {
                _mobileNumber = value;
                OnPropertyChanged();
            }
        }

        public string Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                OnPropertyChanged();
            }
        }

        public string Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                OnPropertyChanged();
            }
        }

        public Command LoadUserProfileCommand { get; }
        public Command SubmitRequestCommand { get; }

        private async Task LoadUserDetailsAsync()
        {
            // Retrieve the mobile number from preferences
            var mobileNumber = Preferences.Get("mobile_number", string.Empty);

            if (!string.IsNullOrEmpty(mobileNumber))
            {
                try
                {
                    // Retrieve user details
                    var users = await _firebaseClient
                        .Child("users")
                        .OnceAsync<Users>();

                    var user = users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber)?.Object;

                    if (user != null)
                    {
                        FirstName = user.FullName;
                        MobileNumber = user.MobileNumber;

                        // Retrieve vehicle details using the user ID (mobile number)
                        var vehicles = await _firebaseClient
                            .Child("vehicles")
                            .OnceAsync<Vehicle>();

                        var vehicle = vehicles.FirstOrDefault(v => v.Object.UserId == user.UserId.ToString())?.Object;

                        if (vehicle != null)
                        {
                            VehicleDescription = vehicle.VehicleDescription;
                            PlateNumber = vehicle.PlateNumber;
                        }
                        else
                        {
                            // Handle the case where the vehicle is not found
                            await Application.Current.MainPage.DisplayAlert("Error", "Vehicle not found.", "OK");
                        }
                    }
                    else
                    {
                        // Handle the case where the user is not found
                        await Application.Current.MainPage.DisplayAlert("Error", "User not found.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions, such as network errors
                    await Application.Current.MainPage.DisplayAlert("Error", "Unable to load user data. Please check your internet connection.", "OK");
                }
            }
            else
            {
                // Handle the case where the mobile number is not found in preferences
                await Application.Current.MainPage.DisplayAlert("Error", "Mobile number not found in preferences.", "OK");
            }
        }

        private async Task GetLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    Latitude = location.Latitude.ToString();
                    Longitude = location.Longitude.ToString();
                }
                else
                {
                    // Handle case when location is null
                    await Application.Current.MainPage.DisplayAlert("Warning", "Unable to get location. Please try again.", "OK");
                }
            }
            catch (FeatureNotSupportedException)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Geolocation is not supported on this device.", "OK");
            }
            catch (FeatureNotEnabledException)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Geolocation is not enabled on this device.", "OK");
            }
            catch (PermissionException)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Geolocation permissions are denied.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Unable to get location: {ex.Message}", "OK");
            }
        }

        private async Task SubmitRequestAsync()
        {
            if (string.IsNullOrEmpty(MobileNumber))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "There was a problem with your mobile number", "OK");
                return;
            }

            // Get the user's current location
            await GetLocationAsync();
            await MopupService.Instance.PopAsync();
            await Shell.Current.GoToAsync($"//{nameof(LoadingPage)}");

            // Generate a unique request ID
            /* var requestId = Guid.NewGuid().ToString();

             // Create a new request object
             var newRequest = new Request
             {
                 RequestId = requestId,
                 UserId = MobileNumber,
                 ServiceProviderId = null, // Initially null until a provider accepts
                 Latitude = Latitude, // Get the latitude from the property
                 Longitude = Longitude, // Get the longitude from the property
                 Date = DateTime.UtcNow,
                 Status = "Pending",
             };

             // Save the request to the PendingRequests table in Firebase
             await _firebaseClient2
                 .Child("PendingRequests")
                 .Child(requestId)
                 .PutAsync(newRequest);*/

            // Find nearby service providers after saving the request
            //  await FindNearbyServiceProvidersAsync(requestId, Latitude, Longitude);
        }

        
       /* private async Task FindNearbyServiceProvidersAsync(string requestId, string userLatitude, string userLongitude)
        {
            var serviceProviders = await _firebaseClient2
                .Child("working")
                .OnceAsync<Working>();

            // Convert the user's latitude and longitude to double
            double userLat = Convert.ToDouble(userLatitude);
            double userLon = Convert.ToDouble(userLongitude);

            // List to store nearby service providers
            List<Working> nearbyProviders = new List<Working>();

            // Loop through each service provider in the working table
            foreach (var provider in serviceProviders)
            {
                double providerLat = Convert.ToDouble(provider.Object.Latitude);
                double providerLon = Convert.ToDouble(provider.Object.Longitude);

                // Calculate the distance between user and provider
                double distance = CalculateDistance(userLat, userLon, providerLat, providerLon);

                // Assume 10km as the radius within which we consider service providers
                if (distance <= 10) // 10 km radius
                {
                    nearbyProviders.Add(provider.Object); // Add the provider object to the list
                }
            }

            // Notify these providers about the new request
           foreach (var provider in nearbyProviders)
           {
                // Notify or update status in Firebase (this can be further expanded)
                await NotifyServiceProviderAsync(provider, requestId);
           }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula to calculate the distance between two points on the earth
            var R = 6371; // Radius of the earth in km
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c; // Distance in km
            return distance;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }


        private async Task NotifyServiceProviderAsync(Working provider, string requestId)
        {
            // Prepare the notification payload
            var notificationPayload = new
            {
                to = provider.FcmToken,  // Ensure the provider has an FCM token stored in Firebase
                notification = new
                {
                    title = "New Roadside Request",
                    body = $"You have a new request. Request ID: {requestId}",
                    sound = "default"
                },
                data = new
                {
                    requestId = requestId,
                    latitude = Latitude,  // Provide the necessary request details
                    longitude = Longitude
                }
            };

            // Send the notification via Firebase Cloud Messaging API
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("key", "U18eMCEHvsS70DgrgZXGpQK1kpymwlJ9FA7kyNM-2mU");

            var content = new StringContent(JsonConvert.SerializeObject(notificationPayload), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", content);

            if (response.IsSuccessStatusCode)
            {
                await Application.Current.MainPage.DisplayAlert("Notification Sent", $"Notification sent to {provider.Id}.", "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to send notification.", "OK");
            }
        }
*/
        
    }
}
