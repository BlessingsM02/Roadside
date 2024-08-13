using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.ObjectModel;
using System.Windows.Input;


namespace Roadside.ViewModels
{
    public class LoadingViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;
        public ICommand ButtonClickedCommand { get; }
        private ObservableCollection<WorkingWithUser> _allWorking;

        public LoadingViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            LoadAllWorkingCommand = new Command(async () => await LoadAllWorkingAsync());
            LoadAllWorkingCommand.Execute(null);

            // Initialize the ButtonClickedCommand to accept a parameter of type WorkingWithUser
            ButtonClickedCommand = new Command<WorkingWithUser>(OnButtonClicked);
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
                        DriverId = mobileNumber
                    };

                    // Send the object to Firebase and get the key
                    var result = await _firebaseClient
                        .Child("ClickedMobileNumbers")
                        .PostAsync(requestData);

                    string key = result.Key; // The key of the newly created record
                    await Application.Current.MainPage.DisplayAlert("Success", "Data sent to Firebase.", "OK");

                    // Schedule a task to delete the record after 1 minute if still "Pending"
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    await CheckAndDeletePendingRecord(key);


                }
                catch (Exception ex)
                {
                    // Handle exceptions here
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to send data: {ex.Message}", "OK");
                }
            }
        }

        private async Task CheckAndDeletePendingRecord(string key)
        {
            try
            {
                // Retrieve the record by key
                var record = await _firebaseClient
                    .Child("ClickedMobileNumbers")
                    .Child(key)
                    .OnceSingleAsync<dynamic>();

                // Check if the status is still "Pending"
                if (record != null && record.Status == "Pending")
                {
                    // Delete the record
                    await _firebaseClient
                        .Child("ClickedMobileNumbers")
                        .Child(key)
                        .DeleteAsync();

                    await Application.Current.MainPage.DisplayAlert("Info", "Service Provide did not respond", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                await Application.Current.MainPage.DisplayAlert("Error", "Something went wrong", "OK");
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
