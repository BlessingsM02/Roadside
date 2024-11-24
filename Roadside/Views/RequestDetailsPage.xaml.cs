using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Mopups.Services;
using Plugin.LocalNotification;
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
                await ShowNotification("Completed", $"Service has been completed, Total price is {currentServiceRequest.Object.Price}");
                await ShowPriceAndRatingDialog(currentServiceRequest.Object.Price, currentServiceRequest.Object.DriverId);

                // Optionally delete the request after showing the dialog
                await _firebaseClient.Child("request").Child(_currentRequestKey).DeleteAsync();

                await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                return;
            }
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Error", "No active request found.", "OK");
            await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            return;
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
            //await Application.Current.MainPage.DisplayAlert("Alert", "Location", "OK");
            await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            return;
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

    private async Task ShowNotification(string title, string description)
    {
        var tes = new NotificationRequest
        {
            NotificationId = 134,
            Title = title,
            Description = description,
            BadgeNumber = 42,

        };
        await LocalNotificationCenter.Current.Show(tes);
    }

    private async Task ShowPriceAndRatingDialog(double price, string driverId)
    {
        // Display the price to the user
        string priceMessage = $"The service is completed. The total price is {price:C}.";
        await Application.Current.MainPage.DisplayAlert("Service Completed", priceMessage, "OK");

        // Show a custom rating popup
        var ratingPopup = new RatingPopup();
        await MopupService.Instance.PushAsync(ratingPopup);

        // Wait for user input
        int? selectedRating = await ratingPopup.GetRatingAsync();

        if (selectedRating.HasValue && selectedRating >= 1 && selectedRating <= 5)
        {
            // Retrieve the current request
            var currentRequest = await _firebaseClient
                .Child("request")
                .Child(_currentRequestKey)
                .OnceSingleAsync<RequestData>();

            // Create a new rating object
            var rating = new Rating
            {
                DriverId = currentRequest.DriverId,
                ServiceProviderId = currentRequest.ServiceProviderId,
                RatingValue = selectedRating.Value,
                RequestId = _currentRequestKey,
                Timestamp = DateTime.UtcNow
            };

            // Save the rating to the Firebase 'ratings' table
            await _firebaseClient
                .Child("ratings")
                .PostAsync(rating);

            await Application.Current.MainPage.DisplayAlert("Thank You", "Your rating has been submitted.", "OK");

            // Optionally update the request to mark it as rated
            currentRequest.RatingId = selectedRating.Value;
            await _firebaseClient
                .Child("request")
                .Child(_currentRequestKey)
                .PutAsync(currentRequest);
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please select a valid rating.", "OK");
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
                        await MopupService.Instance.PopAsync();
                        return;
                    }
                    else
                    {
                        await MopupService.Instance.PopAsync();
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
            //await Application.Current.MainPage.DisplayAlert("Alert", "Info", "OK");
            await MopupService.Instance.PopAsync();
            await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
        }
    }
}

