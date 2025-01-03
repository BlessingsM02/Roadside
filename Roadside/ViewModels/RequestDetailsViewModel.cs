﻿
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using Roadside.Models;
using Roadside.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Roadside.ViewModels
{
    public class RequestDetailsViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly FirebaseClient _firebaseClient2;
        private string _serviceProviderId;
        private double _latitude;
        private double _longitude;
        private double _serviceProviderLatitude;
        private double _serviceProviderLongitude;
        private double _price;
        private string _driverId;
        private string _status;
        private int _ratingId;
        private DateTime _date;
        private string _driverName;
        private string _vehicleDetails;


        public RequestDetailsViewModel()
        {
            CancelRequestCommand = new Command(async () => await CancelRequestAsync());
            OpenDialerCommand = new Command<string>(OpenDialer);
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            _firebaseClient2 = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
            LoadRequestDetailsCommand = new Command(async () => await LoadRequestDetailsAsync());
        }

        public ICommand OpenDialerCommand { get; }
        public ICommand CancelRequestCommand { get; }

        private void OpenDialer(string phoneNumber)
        {
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                try
                {
                    PhoneDialer.Open(phoneNumber);
                }
                catch (Exception ex)
                {
                    Application.Current.MainPage.DisplayAlert("Error", "Unable to open the dialer.", "OK");
                }
            }
        }

        private async Task CancelRequestAsync()
        {
            try
            {
                // Prompt the user for a reason
                string reason = await Application.Current.MainPage.DisplayPromptAsync(
                    "Cancel Request",
                    "Please provide a reason for canceling the request:",
                    accept: "OK",
                    cancel: "Cancel"
                );

                // If the user provides a reason, proceed with the cancellation
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                    // Retrieve the request to be canceled
                    var requestDetails = await _firebaseClient
                        .Child("request")
                        .OnceAsync<RequestData>();

                    var requestData = requestDetails.FirstOrDefault(r => r.Object.DriverId == mobileNumber)?.Object;

                    if (requestData != null)
                    {
                        // Update the request status to "Canceled" and add the cancellation reason
                        var canceledRequestKey = requestDetails.First(r => r.Object.DriverId == mobileNumber).Key;

                        await _firebaseClient
                            .Child("Canceled")
                            .Child(canceledRequestKey)
                            .PutAsync(new RequestData
                            {
                                ServiceProviderId = requestData.ServiceProviderId,
                                Latitude = requestData.Latitude,
                                Longitude = requestData.Longitude,
                                ServiceProviderLatitude = requestData.ServiceProviderLatitude,
                                ServiceProviderLongitude = requestData.ServiceProviderLongitude,
                                Price = requestData.Price,
                                DriverId = requestData.DriverId,
                                Status = "Canceled",  // Mark the request as canceled
                                RatingId = requestData.RatingId,
                                Date = requestData.Date,
                                CancellationReason = reason // Add the cancellation reason
                            });

                        // Now delete the original request
                        await _firebaseClient
                            .Child("request")
                            .Child(canceledRequestKey)
                            .DeleteAsync();

                        await Application.Current.MainPage.DisplayAlert("Success", "The request has been canceled", "OK");
                        await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "No matching request found to cancel.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to cancel request: {ex.Message}", "OK");
            }
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

        public double Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                OnPropertyChanged();
            }
        }

        public double Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                OnPropertyChanged();
            }
        }

        public double ServiceProviderLatitude
        {
            get => _serviceProviderLatitude;
            set
            {
                _serviceProviderLatitude = value;
                OnPropertyChanged();
            }
        }

        public double ServiceProviderLongitude
        {
            get => _serviceProviderLongitude;
            set
            {
                _serviceProviderLongitude = value;
                OnPropertyChanged();
            }
        }

        public double Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
            }
        }

        public string DriverId
        {
            get => _driverId;
            set
            {
                _driverId = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public int RatingId
        {
            get => _ratingId;
            set
            {
                _ratingId = value;
                OnPropertyChanged();
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        public string DriverName
        {
            get => _driverName;
            set
            {
                _driverName = value;
                OnPropertyChanged();
            }
        }

        public string VehicleDetails
        {
            get => _vehicleDetails;
            set
            {
                _vehicleDetails = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadRequestDetailsCommand { get; }

        public async Task LoadRequestDetailsAsync()
        {
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                // Retrieve request details from the requests table
                var requestDetails = await _firebaseClient
                    .Child("request")
                    .OnceAsync<RequestData>();
                //retrieve user data
                var specificUser = await _firebaseClient2
                    .Child("users")
                    .OnceAsync<Users>();


                var requestData = requestDetails.FirstOrDefault(r => r.Object.DriverId == mobileNumber)?.Object;
                var currentUser = specificUser.FirstOrDefault(c => c.Object.MobileNumber == requestData.ServiceProviderId)?.Object; //Specific user
                if (requestData != null)
                {
                    ServiceProviderId = requestData.ServiceProviderId;
                    Latitude = requestData.Latitude;
                    Longitude = requestData.Longitude;
                    ServiceProviderLatitude = requestData.ServiceProviderLatitude;
                    ServiceProviderLongitude = requestData.ServiceProviderLongitude;
                    Price = requestData.Price;
                    DriverId = requestData.DriverId;
                    Status = requestData.Status;
                    RatingId = requestData.RatingId;
                    Date = requestData.Date;

                    // Retrieve driver name from the users table
                    await LoadDriverNameAsync(requestData.ServiceProviderId);

                    // Retrieve vehicle details from the vehicle table
                    await LoadVehicleDetailsAsync(requestData.ServiceProviderId);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No matching request found.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to load request details", "OK");
            }
        }

        private async Task LoadDriverNameAsync(string driverId)
        {
            try
            {
                var userDetails = await _firebaseClient
                    .Child("users")
                    .OnceAsync<Users>();

                var user = userDetails.FirstOrDefault(u => u.Object.MobileNumber == ServiceProviderId);

                if (user != null)
                {
                    var driverDetails = $"{user.Object.FullName}";
                    DriverName = driverDetails; 
                }
                else
                {
                    DriverName = "????";
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load driver name: {ex.Message}", "OK");
            }
        }

        private async Task LoadVehicleDetailsAsync(string serviceProviderId)
        {
            try
            {
                // Query the users table to get the service provider details by ServiceProviderId
                var userDetails = await _firebaseClient
                    .Child("users")
                    .OnceAsync<Users>();

                var user = userDetails.FirstOrDefault(u => u.Object.MobileNumber == serviceProviderId);

                if (user != null)
                {
                    // Once we have the user, query the vehicle details using their UserId
                    var vehicleDetails = await _firebaseClient
                        .Child("vehicles")
                        .OnceAsync<Vehicle>();

                    var vehicle = vehicleDetails.FirstOrDefault(v => v.Object.UserId == user.Object.UserId.ToString());

                    if (vehicle != null)
                    {
                        VehicleDetails = $"{vehicle.Object.VehicleDescription} {vehicle.Object.PlateNumber}";
                    }
                    else
                    {
                        VehicleDetails = "No Vehicle Details Found";
                    }
                }
                else
                {
                    VehicleDetails = "User not found";
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load vehicle details: {ex.Message}", "OK");
            }
        }
        
    }
}
