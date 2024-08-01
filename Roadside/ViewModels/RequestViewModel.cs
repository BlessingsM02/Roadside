using Firebase.Database;
using Firebase.Database.Query;
using Roadside.Models;
using Roadside.Views;

namespace Roadside.ViewModels
{
    internal class RequestViewModel : BindableObject
    {
        private string _firstName;
        private string _lastName;
        private string _vehicleDescription;
        private string _plateNumber;
        private string _mobileNumber;
        private string _latitude;
        private string _longitude;
        private readonly FirebaseClient _firebaseClient;

        public RequestViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
            LoadUserProfileCommand = new Command(async () => await LoadUserDetailsAsync());
            SubmitRequestCommand = new Command(async () => await SubmitRequestAsync());
            LoadUserProfileCommand.Execute(null);
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
            var mobileNumber = Preferences.Get("mobile_number", string.Empty);

            if (!string.IsNullOrEmpty(mobileNumber))
            {
                var users = await _firebaseClient
                    .Child("users")
                    .OnceAsync<Users>();

                var user = users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber)?.Object;

                if (user != null)
                {
                    FirstName = user.FirstName;
                    LastName = user.LastName;
                    MobileNumber = user.MobileNumber;

                    var vehicles = await _firebaseClient
                        .Child("vehicles")
                        .OnceAsync<Vehicle>();

                    var vehicle = vehicles.FirstOrDefault(v => v.Object.UserId == mobileNumber)?.Object;

                    if (vehicle != null)
                    {
                        VehicleDescription = vehicle.VehicleDescription;
                        PlateNumber = vehicle.PlateNumber;
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Vehicle not found.", "OK");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "User not found.", "OK");
                }
            }
            else
            {
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
                    location = await Geolocation.GetLocationAsync(request);

                    if (location != null)
                    {
                        Latitude = location.Latitude.ToString();
                        Longitude = location.Longitude.ToString();
                    }
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
                await Application.Current.MainPage.DisplayAlert("Error", "Mobile number is required.", "OK");
                return;
            }

            await GetLocationAsync();

            if (string.IsNullOrEmpty(Latitude) || string.IsNullOrEmpty(Longitude))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Unable to get location.", "OK");
                return;
            }

            var existingRequests = await _firebaseClient
                .Child("requests")
                .OnceAsync<Request>();

            var existingRequest = existingRequests.FirstOrDefault(r => r.Object.MobileNumber == MobileNumber && r.Object.Status == "Pending");

            if (existingRequest != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "You already have a pending request.", "OK");
                return;
            }

            var request = new Request
            {
                RequestId = Guid.NewGuid().ToString(),
                MobileNumber = MobileNumber,
                Latitude = Latitude,
                Longitude = Longitude,
                Date = DateTime.UtcNow,
                Status = "Pending"
            };

            try
            {
                await _firebaseClient
                    .Child("requests")
                    .PostAsync(request);

                await Application.Current.MainPage.DisplayAlert("Success", "Request submitted successfully.", "OK");
                await App.Current.MainPage.Navigation.PushAsync(new LoadingPage());
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}
