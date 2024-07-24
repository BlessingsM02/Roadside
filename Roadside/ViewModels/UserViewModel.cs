using Firebase.Auth;
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
            SubmitCommand = new Command(async () => await SubmitAsync());
            _firebaseClient = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
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

        private async Task SubmitAsync()
        {
            // Retrieve the mobile number from preferences
            var mobileNumber = Preferences.Get("mobile_number", string.Empty);

            if (!string.IsNullOrEmpty(mobileNumber))
            {
                // Check if a user with the same mobile number already exists
                var users = await _firebaseClient
                    .Child("users")
                    .OnceAsync<Users>();

                var userExists = users.Any(u => u.Object.MobileNumber == mobileNumber);

                if (userExists)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "A user with this mobile number already exists.", "OK");
                    await Shell.Current.GoToAsync($"//{nameof(ProfilePage)}");
                }
                else
                {
                    var user = new Users
                    {
                        FirstName = FirstName,
                        LastName = LastName,
                        VehicleDescription = VehicleDescription,
                        PlateNumber = PlateNumber,
                        MobileNumber = mobileNumber // Add mobile number to the user data
                    };

                    await _firebaseClient
                        .Child("users")
                        .PostAsync(user);

                    await Application.Current.MainPage.DisplayAlert("Success", "User information saved successfully.", "OK");
                }
            }
            else
            {
                // Handle the case where mobile number is not found in preferences
                await Application.Current.MainPage.DisplayAlert("Error", "Mobile number not found in preferences.", "OK");
            }
        }

    }
}
