using Firebase.Database;
using System.Collections.ObjectModel;


namespace Roadside.ViewModels
{
    public class LoadingViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;
        private ObservableCollection<WorkingWithUser> _allWorking;

        public LoadingViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            LoadAllWorkingCommand = new Command(async () => await LoadAllWorkingAsync());
            LoadAllWorkingCommand.Execute(null);
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
                var workingRecords = await _firebaseClient
                    .Child("working")
                    .OnceAsync<Working>();

                var allWorkingWithUser = new ObservableCollection<WorkingWithUser>();

                foreach (var record in workingRecords)
                {
                    var user = await GetUserDetailsAsync(record.Object.Id);
                    allWorkingWithUser.Add(new WorkingWithUser
                    {
                        Id = record.Object.Id,
                        Latitude = record.Object.Latitude,
                        Longitude = record.Object.Longitude,
                        FirstName = user?.FirstName,
                        LastName = user?.LastName,
                        MobileNumber = user?.MobileNumber
                    });
                }

                AllWorking = allWorkingWithUser;
            }
            catch (Exception ex)
            {
                // Handle exception
                await Application.Current.MainPage.DisplayAlert("Error", $"Unable to load data: {ex.Message}", "OK");
            }
        }

        private async Task<User> GetUserDetailsAsync(string mobileNumber)
        {
            try
            {
                var users = await _firebaseClient
                    .Child("users")
                    .OnceAsync<User>();

                return users.FirstOrDefault(u => u.Object.MobileNumber == mobileNumber)?.Object;
            }
            catch (Exception ex)
            {
                // Handle exception
                await Application.Current.MainPage.DisplayAlert("Error", $"Unable to load user details: {ex.Message}", "OK");
                return null;
            }
        }
    }

    public class WorkingWithUser
    {
        public string Id { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MobileNumber { get; set; }
    }

    public class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MobileNumber { get; set; }
    }

    public class Working
    {
        public string Id { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
