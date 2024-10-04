using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Mopups.Services;
using Roadside.Models;

namespace Roadside.Views;
public partial class RequestDetailsPage : ContentPage
{
    private readonly FirebaseClient _firebaseClient;
    private CancellationTokenSource _cancellationTokenSource;
    private Location _userLocation;
    private Location _serviceProviderLocation;

    public RequestDetailsPage()
    {
        InitializeComponent();
        _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _cancellationTokenSource = new CancellationTokenSource();

        // Start updating location every 5 seconds
        StartLocationUpdates();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        //_cancellationTokenSource?.Cancel(); // Stop location updates when page disappears
    }

    private async void StartLocationUpdates()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            await UpdateLocationAsync();
            await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
        }
    }
    private void UpdateMapWithLocation(string label, double latitude, double longitude, bool isUserLocation)
    {
        // Create a new pin for the location
        var pin = new Pin
        {
            Label = label,
            Location = new Location(latitude, longitude),
            Type = PinType.Place
        };

        // If it's the user's location, clear previous pins and add the new pin
        if (isUserLocation)
        {
            userMap.Pins.Clear();
        }

        // Add the pin to the map
        userMap.Pins.Add(pin);
    }

    private async Task UpdateLocationAsync()
    {
        try
        {
            // Fetch the user's current location
            var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(geolocationRequest);

            if (location != null)
            {
                _userLocation = location;

                // Retrieve mobile number
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                // Retrieve the current service request from Firebase
                var serviceRequests = await _firebaseClient
                    .Child("requests")
                    .OnceAsync<RequestData>();

                var currentServiceRequest = serviceRequests
                    .FirstOrDefault(u => u.Object.DriverId == mobileNumber);

                if (currentServiceRequest != null)
                {
                    var requestStatus = currentServiceRequest.Object.Status;

                    if (requestStatus == "Accepted")
                    {
                        // Update user's location in Firebase
                        currentServiceRequest.Object.Latitude = location.Latitude;
                        currentServiceRequest.Object.Longitude = location.Longitude;

                        await _firebaseClient
                            .Child("requests")
                            .Child(currentServiceRequest.Key)
                            .PutAsync(currentServiceRequest.Object);

                        // Update user's and service provider's locations on the map
                        UpdateMapWithLocation("Your Location", location.Latitude, location.Longitude, true);

                        if (currentServiceRequest.Object.ServiceProviderLatitude != 0 && currentServiceRequest.Object.ServiceProviderLongitude != 0)
                        {
                            var serviceProviderLatitude = currentServiceRequest.Object.ServiceProviderLatitude;
                            var serviceProviderLongitude = currentServiceRequest.Object.ServiceProviderLongitude;

                            _serviceProviderLocation = new Location(serviceProviderLatitude, serviceProviderLongitude);
                            UpdateMapWithLocation("Service Provider Location", serviceProviderLatitude, serviceProviderLongitude, false);

                            // Draw the route between user and service provider
                            DrawRoute(_userLocation, _serviceProviderLocation);
                        }
                    }
                    else if (requestStatus == "Completed")
                    {
                       
                        // Show the price and ask the user to rate the driver
                        await ShowPriceAndRatingDialog(currentServiceRequest.Object.Amount, currentServiceRequest.Object.DriverId);

                        await _firebaseClient
                              .Child("requests")
                              .Child(currentServiceRequest.Key)
                              .DeleteAsync();
                        return;

                        
                    }
                    else
                    {
                        // Notify the user if the status has changed
                        await Application.Current.MainPage.DisplayAlert("Status Update", $"Request status has changed to {requestStatus}.", "OK");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No active request found.", "OK");
                    try
                    {
                        
                        await Shell.Current.GoToAsync($"//{nameof(HomePage)}"); 
                    }
                    catch (Exception ex) {
                        await Application.Current.MainPage.DisplayAlert("Error", "Nav eerr.", "OK");

                    }

                    return;

                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Unable to retrieve location data.", "OK");
            }
        }
        catch (Exception geoEx)
        {
            await Application.Current.MainPage.DisplayAlert("Location Error", $"Failed to retrieve location: {geoEx.Message}", "OK");
        }
       
    }

    // Method to display price and ask for driver rating
    private async Task ShowPriceAndRatingDialog(double price, string driverId)
    {
        // Display the price
        await Application.Current.MainPage.DisplayAlert("Price", $"The service costs {price:C}.", "OK");

        // Ask the user to rate the driver
        string rating = await Application.Current.MainPage.DisplayPromptAsync(
            "Rate Driver",
            "Please rate the driver from 1 to 5 stars:",
            maxLength: 1,
            keyboard: Keyboard.Numeric);

        if (int.TryParse(rating, out int ratingValue) && ratingValue >= 1 && ratingValue <= 5)
        {
            // Update Firebase with the driver rating
            await UpdateDriverRating(driverId, ratingValue);
            await Application.Current.MainPage.DisplayAlert("Thank You", "Your rating has been submitted!", "OK");
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Invalid Rating", "Please provide a valid rating between 1 and 5.", "OK");
        }
    }

    // Method to update the driver's rating in Firebase
    private async Task UpdateDriverRating(string driverId, int ratingValue)
    {
        try
        {
            var driverRatingData = new RatingData
            {
                DriverId = driverId,
                Rating = ratingValue,
                Date = DateTime.UtcNow
            };

            await _firebaseClient
                .Child("driverRatings")
                .PostAsync(driverRatingData);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to submit rating: {ex.Message}", "OK");
        }
    }


    private void DrawRoute(Location startLocation, Location endLocation)
    {
        var routeLine = new Polyline
        {
            StrokeColor = Colors.Blue,
            StrokeWidth = 3
        };

        // Assuming we use straight-line points for the route (a real implementation would require route data)
        routeLine.Geopath.Add(startLocation);
        routeLine.Geopath.Add(endLocation);

        userMap.MapElements.Clear();
        userMap.MapElements.Add(routeLine);
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var bottomSheet = new RequestDetailsBottomSheet();
        await MopupService.Instance.PushAsync(bottomSheet);
    }
}

internal class RatingData
{
    public string DriverId { get; set; }
    public int Rating { get; set; }
    public DateTime Date { get; set; }
}