﻿using Firebase.Database;
using Firebase.Database.Query;
using Roadside.Views;
using Roadside.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Roadside.ViewModels
{
    public class LoadingViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;
        private bool _isRefreshing;
        public ICommand ButtonClickedCommand { get; }
        private ObservableCollection<WorkingWithUser> _allWorking;

        public LoadingViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            LoadAllWorkingCommand = new Command(async () => await LoadAllWorkingAsync());
            LoadAllWorkingCommand.Execute(null);
            RefreshCommand = new Command(async () => await OnRefresh());
            // Initialize the ButtonClickedCommand to accept a parameter of type WorkingWithUser
            ButtonClickedCommand = new Command<WorkingWithUser>(OnButtonClicked);
        }

        public ICommand RefreshCommand { get; }
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }

        private async Task OnRefresh()
        {
            IsRefreshing = true;
            await LoadAllWorkingAsync();
            IsRefreshing = false;
        }
        private async void OnButtonClicked(WorkingWithUser selectedUser)
        {
            if (selectedUser != null)
            {
                try
                {
                    var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    var location = await Geolocation.GetLocationAsync(request);

                    var requestData = new
                    {
                        ServiceProviderId = selectedUser.MobileNumber,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Status = "Pending",
                        Date = DateTime.UtcNow.ToString("o"),
                        DriverId = mobileNumber,
                        Price = selectedUser.Price, // Use pre-calculated price
                        RatingId = 0
                    };

                    // Send the object to Firebase and get the key
                    var result = await _firebaseClient
                        .Child("request")
                        .PostAsync(requestData);

                    string key = result.Key; // The key of the newly created record

                    // Pass the key to the ResponsePage and navigate to response page
                    await App.Current.MainPage.Navigation.PushAsync(new ResponsePage(key));
                }
                catch (Exception ex)
                {
                    // Handle exceptions here
                    await Application.Current.MainPage.DisplayAlert("Error", "There was a problem making a request, try again later", "OK");
                }
            }
        }

        public ObservableCollection<WorkingWithUser> AllWorking
        {
            get => _allWorking;
            set
            {
                _allWorking = value;
                OnPropertyChanged();
            }
        }

        public Command LoadAllWorkingCommand { get; }

        private async Task LoadAllWorkingAsync()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var userLocation = await Geolocation.GetLocationAsync(request);

                if (userLocation == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Unable to retrieve your location.", "OK");
                    return;
                }

                var workingRecords = await _firebaseClient
                    .Child("working")
                    .OnceAsync<Working>();

                var allWorkingWithUser = new ObservableCollection<WorkingWithUser>();

                foreach (var record in workingRecords)
                {
                    var serviceProviderLatitude = double.Parse(record.Object.Latitude);
                    var serviceProviderLongitude = double.Parse(record.Object.Longitude);

                    double distanceInKm = CalculateDistance(
                        userLocation.Latitude,
                        userLocation.Longitude,
                        serviceProviderLatitude,
                        serviceProviderLongitude
                    );

                    if (distanceInKm <= 16)
                    {
                        var user = await GetUserDetailsAsync(record.Object.Id);
                        if (user != null)
                        {
                            double price = CalculatePrice(distanceInKm);

                            // Fetch and calculate average rating
                            double averageRating = await GetAverageRatingAsync(record.Object.Id);

                            allWorkingWithUser.Add(new WorkingWithUser
                            {
                                Id = record.Object.Id,
                                Latitude = record.Object.Latitude,
                                Longitude = record.Object.Longitude,
                                FullName = user.FullName,
                                MobileNumber = user.MobileNumber,
                                Price = price,
                                AverageRating = averageRating // Add calculated average rating
                            });
                        }
                    }
                }

                AllWorking = allWorkingWithUser;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Unable to load data: {ex.Message}", "OK");
            }
        }

        private async Task<double> GetAverageRatingAsync(string serviceProviderId)
        {
            try
            {
                var ratings = await _firebaseClient
                    .Child("ratings")
                    .OnceAsync<Rating>(); // Assuming Rating is a class representing the ratings

                var serviceProviderRatings = ratings
                    .Where(r => r.Object.ServiceProviderId == serviceProviderId)
                    .Select(r => r.Object.RatingValue);

                if (!serviceProviderRatings.Any())
                    return 0;

                return Math.Round(serviceProviderRatings.Average(), 1); // Calculate average and round to 1 decimal place
            }
            catch
            {
                return 0; // Return 0 if there is an error or no ratings
            }
        }


        private async Task<Users> GetUserDetailsAsync(string mobileNumber)
        {
            try
            {
                var users = await _firebaseClient
                    .Child("users")
                    .OnceAsync<Users>();
                return users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber)?.Object;
            }
            catch (Exception ex)
            {
                // Handle exception
                await Application.Current.MainPage.DisplayAlert("Error", $"Unable to load user details: {ex.Message}", "OK");
                return null;
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radius of Earth in kilometers
            var latRad1 = DegreesToRadians(lat1);
            var latRad2 = DegreesToRadians(lat2);
            var deltaLat = DegreesToRadians(lat2 - lat1);
            var deltaLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(latRad1) * Math.Cos(latRad2) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Distance in kilometers
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private double CalculatePrice(double distanceInKm)
        {
            double baseFare = 25.00; //base fare
            double ratePerKm = 12.00; //rate per kilometer

            double price = baseFare + (ratePerKm * distanceInKm);

            // Format to 2 decimal places and return
            return Math.Round(price, 2);
        }
    }

    public class WorkingWithUser
    {
        public string Id { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string MobileNumber { get; set; }
        public string FullName { get; set; }
        public double Price { get; set; }
        public double AverageRating { get; set; }
    }



    public class Working
    {
        public string Id { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
