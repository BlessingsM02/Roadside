using Firebase.Database;
using Roadside.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;


namespace Roadside.Views
{
    public partial class RequestDetailsPage : ContentPage
    {

        private readonly FirebaseClient _firebaseClient;

        public RequestDetailsPage()
        {
            InitializeComponent();
            _firebaseClient = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");

        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();


            var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromMicroseconds(20));
            var location = await Geolocation.GetLocationAsync(geolocationRequest);

            userMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));

            var mobileNumber = Preferences.Get("mobile_number", string.Empty);
            var servicelocation = await _firebaseClient
                .Child("request")
                .OnceAsync<RequestData>();

            var currentServiceLocation = servicelocation.FirstOrDefault(u => u.Object.DriverId == mobileNumber);

            if (currentServiceLocation != null)
            {
                await Application.Current.MainPage.DisplayAlert("Info", $"{currentServiceLocation.Object.ServiceProviderLatitude}, {currentServiceLocation.Object.ServiceProviderLongitude}", "OK");

                // Convert latitude and longitude from string to double
                if (double.TryParse(currentServiceLocation.Object.ServiceProviderLatitude, out double latitude) &&
                    double.TryParse(currentServiceLocation.Object.ServiceProviderLongitude, out double longitude))
                {
                    var loc = new Microsoft.Maui.Devices.Sensors.Location(latitude, longitude);

                    var pin = new Pin
                    {
                        Address = $"{longitude}, {latitude}",
                        Location = loc,
                        Type = PinType.Place,
                        Label = "Current Location",
                    };

                    // Assuming 'map' is your map control
                    userMap.Pins.Add(pin);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "There was a problem fetching service provider location", "OK");
                    // Handle cases where latitude or longitude could not be parsed
                    // For example, you could log an error or display a message
                }



            }





        }
    }
}