using Firebase.Database;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Mopups.Services;
using Roadside.Models;

namespace Roadside.Views
{
    public partial class RequestDetailsPage : ContentPage
    {
        private readonly FirebaseClient _firebaseClient;

        public RequestDetailsPage()
        {
            InitializeComponent();
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Fetch the user's current location
            var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(geolocationRequest);

            if (location != null)
            {
                // Center the map on the user's location
                userMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));

                // Add a pin for the user's current location
                var userPin = new Pin
                {
                    Label = "Your Location",
                    Location = new Location(location.Latitude, location.Longitude),
                    Type = PinType.Place
                };
                userMap.Pins.Add(userPin);
            }
            else
            {
                await DisplayAlert("Error", "Unable to get current location.", "OK");
                return;
            }

            // Retrieve the service provider's coordinates from the "requests" table in Firebase
            var serviceLocation = await _firebaseClient
                .Child("requests")
                .OnceAsync<RequestData>(); // Using dynamic instead of a specific type
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                var currentServiceLocation = serviceLocation.FirstOrDefault(u => u.Object.DriverId == mobileNumber);

                if (currentServiceLocation != null)
                {
                    var latitude = currentServiceLocation.Object.ServiceProviderLatitude;
                    var longitude = currentServiceLocation.Object.ServiceProviderLongitude;

                    var servicePin = new Pin
                    {
                        Label = "Service Provider Location",
                        Location = new Location(latitude, longitude),
                        Type = PinType.Place
                    };
                    userMap.Pins.Add(servicePin);
                }
                else
                {
                    await DisplayAlert("Error", "No matching request found.", "OK");
                }
            }
            catch (Exception ex) 
            {
                await DisplayAlert("Error", "wrong shit", "OK");

            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            var bottomSheet = new RequestDetailsBottomSheet();

            await MopupService.Instance.PushAsync(bottomSheet);
        }
    }
}
