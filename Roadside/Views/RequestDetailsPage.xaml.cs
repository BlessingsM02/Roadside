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
    private string _currentRequestKey; // Store the key of the active request
    private bool _isRequestCompleted = false; // Track if the request is completed

    public RequestDetailsPage()
    {
        InitializeComponent();
        _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _cancellationTokenSource = new CancellationTokenSource();

        await InitializeCurrentRequest();

        // Start updating location every 5 seconds if the request is still active
        if (!_isRequestCompleted)
        {
            StartLocationUpdates();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cancellationTokenSource?.Cancel(); // Stop location updates when page disappears
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var bottomSheet = new RequestDetailsBottomSheet();
        await MopupService.Instance.PushAsync(bottomSheet);
    }
    private async Task InitializeCurrentRequest()
    {
        // Retrieve mobile number
        var mobileNumber = Preferences.Get("mobile_number", string.Empty);

        // Retrieve the current service request from Firebase
        var serviceRequests = await _firebaseClient
            .Child("request")
            .OnceAsync<RequestData>();

        var currentServiceRequest = serviceRequests
            .FirstOrDefault(u => u.Object.DriverId == mobileNumber);

        if (currentServiceRequest != null)
        {
            _currentRequestKey = currentServiceRequest.Key;

            if (currentServiceRequest.Object.Status == "Completed")
            {
                _isRequestCompleted = true; // Set completed status to true
                await ShowPriceAndRatingDialog(currentServiceRequest.Object.Price, currentServiceRequest.Object.DriverId);

                // Optionally delete the request after showing the dialog
                await _firebaseClient.Child("request").Child(_currentRequestKey).DeleteAsync();

                await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Error", "No active request found.", "OK");
            await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
        }
    }

    private async void StartLocationUpdates()
    {
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await UpdateLocationAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException)
        {
            // Handle task cancellation (safe to ignore in this case)
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Alert", "Location", "OK");
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

        // If it's the user's location, clear previous user pins
        if (isUserLocation)
        {
            var userPin = userMap.Pins.FirstOrDefault(p => p.Label == "Your Location");
            if (userPin != null)
            {
                userMap.Pins.Remove(userPin);  // Remove the previous user's pin
            }
            //userMap.Pins.Add(pin);  // Add the new user pin
        }
        else
        {
            // For service provider, clear previous provider pins
            var serviceProviderPin = userMap.Pins.FirstOrDefault(p => p.Label == "Service Provider Location");
            if (serviceProviderPin != null)
            {
                userMap.Pins.Remove(serviceProviderPin);  // Remove the previous service provider's pin
            }
            userMap.Pins.Add(pin);  // Add the new service provider pin
        }

        // Optionally, center the map on the latest location
        //userMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(latitude, longitude), Distance.FromKilometers(1)));
    }

    private async Task ShowPriceAndRatingDialog(double price, string driverId)
    {
        // Display the price to the user
        string priceMessage = $"The service is completed. The total price is {price:C}.";
        await Application.Current.MainPage.DisplayAlert("Service Completed", priceMessage, "OK");

        // Ask for a rating
        string ratingMessage = "Please rate the service provider (1 to 5 stars):";
        string ratingInput = await Application.Current.MainPage.DisplayPromptAsync("Rate Driver", ratingMessage, "Submit", "Cancel", "Enter rating here", 1, Keyboard.Numeric);

        if (!string.IsNullOrEmpty(ratingInput) && int.TryParse(ratingInput, out int rating) && rating >= 1 && rating <= 5)
        {
            // Save the rating to Firebase
            var ratingData = new
            {
                DriverId = driverId,
                Rating = rating,
                Date = DateTime.UtcNow
            };

            // Assuming you have a "ratings" table in Firebase
            await _firebaseClient.Child("ratings").PostAsync(ratingData);

            await Application.Current.MainPage.DisplayAlert("Thank You", "Your rating has been submitted.", "OK");
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Invalid rating input. Please enter a number between 1 and 5.", "OK");
        }
    }

    private async Task UpdateLocationAsync()
    {
        try
        {
            var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(geolocationRequest);

            if (location != null)
            {
                _userLocation = location;

                if (!string.IsNullOrEmpty(_currentRequestKey))
                {
                    var currentRequest = await _firebaseClient.Child("request").Child(_currentRequestKey).OnceSingleAsync<RequestData>();

                    if (currentRequest.Status == "Accepted")
                    {
                        // Update location in Firebase
                        currentRequest.Latitude = location.Latitude;
                        currentRequest.Longitude = location.Longitude;
                        await _firebaseClient.Child("request").Child(_currentRequestKey).PutAsync(currentRequest);

                        // Update map
                        UpdateMapWithLocation("Your Location", location.Latitude, location.Longitude, true);

                        if (currentRequest.ServiceProviderLatitude != 0 && currentRequest.ServiceProviderLongitude != 0)
                        {
                            _serviceProviderLocation = new Location(currentRequest.ServiceProviderLatitude, currentRequest.ServiceProviderLongitude);
                            UpdateMapWithLocation("Service Provider Location", currentRequest.ServiceProviderLatitude, currentRequest.ServiceProviderLongitude, false);
                        }
                    }
                    else if (currentRequest.Status == "Completed")
                    {
                        _isRequestCompleted = true;
                        await ShowPriceAndRatingDialog(currentRequest.Price, currentRequest.DriverId);

                        await _firebaseClient.Child("request").Child(_currentRequestKey).DeleteAsync();
                        return;
                    }
                    else
                    {
                        await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                    }
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Unable to retrieve location data.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update location: {ex.Message}", "OK");
            await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
        }
    }
}

internal class RatingData
{
    public string DriverId { get; set; }
    public int Rating { get; set; }

    public DateTime Date { get; set; }
}