using System.Windows.Input;
using Firebase.Database;
using Firebase.Database.Query;
using Roadside.Models;
using Roadside.Views;


namespace Roadside.ViewModels
{
    internal class UserViewModel : BindableObject
    {
        private string _firstName;
        private string _lastName;
        private string _vehicleDescription;
        private string _plateNumber;
        private FirebaseClient _firebaseClient;

        public UserViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
            SubmitCommand = new Command(async () => await SubmitAsync());
            CheckUserExistsAsync();
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

        public ICommand SubmitCommand { get; }

        private async Task CheckUserExistsAsync()
        {
            var mobileNumber = Preferences.Get("mobile_number", string.Empty);

            if (!string.IsNullOrEmpty(mobileNumber))
            {
                var users = await _firebaseClient
                    .Child("users")
                    .OnceAsync<Users>();

                var user = users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber);

                if (user != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "A user with this mobile number already exists.", "OK");
                    await Shell.Current.GoToAsync($"//{nameof(ProfilePage)}");
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Mobile number not found in preferences.", "OK");
            }
        }

        private async Task SubmitAsync()
        {
            if (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName) ||
                string.IsNullOrEmpty(VehicleDescription) || string.IsNullOrEmpty(PlateNumber))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "All fields are required.", "OK");
                return;
            }

            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                if (!string.IsNullOrEmpty(mobileNumber))
                {
                    bool userExists = await SaveUser(mobileNumber);
                    if (userExists)
                    {
                        await SaveVehicle(mobileNumber);
                        await Application.Current.MainPage.DisplayAlert("Success", "Information saved successfully.", "OK");
                        await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "There was a problem with your mobile number", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task<bool> SaveUser(string mobileNumber)
        {
            var users = await _firebaseClient
                .Child("users")
                .OnceAsync<Users>();

            var user = users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber);

            if (user != null)
            {
                await Application.Current.MainPage.DisplayAlert("Alert", "A user with this mobile number already exists.", "OK");
                await Shell.Current.GoToAsync($"//{nameof(ProfilePage)}");
                return false;
            }

            var newUser = new Users
            {
                FirstName = FirstName,
                LastName = LastName,
                MobileNumber = mobileNumber
            };

            await _firebaseClient
                .Child("users")
                .PostAsync(newUser);

            return true;
        }

        private async Task SaveVehicle(string mobileNumber)
        {
            var vehicle = new Vehicle
            {
                UserId = mobileNumber,
                VehicleDescription = VehicleDescription,
                PlateNumber = PlateNumber
            };

            await _firebaseClient
                .Child("vehicles")
                .PostAsync(vehicle);
        }
    }

}
