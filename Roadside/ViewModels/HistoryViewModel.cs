
using Firebase.Database;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Roadside.Models;
using System.Globalization;
using Firebase.Database.Query;

namespace Roadside.ViewModels
{
    internal class HistoryViewModel : INotifyPropertyChanged
    {
        private readonly FirebaseClient _firebaseClient;
        public ObservableCollection<RequestData> AllRequests { get; private set; }
        public ObservableCollection<RequestData> FilteredRequests { get; private set; }
        public bool IsBusy { get; private set; }
        public string _serviceProviderName { get; set; }

        private double _totalAmount; // Field to hold the total amount
        public double TotalAmount
        {
            get => _totalAmount;
            private set
            {
                _totalAmount = value;
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        public String ServiceProviderName
        {
            get => _serviceProviderName;
            private set
            {
                _serviceProviderName = value;
                OnPropertyChanged(nameof(ServiceProviderName));
            }
        }

        public ICommand RefreshCommand { get; private set; }
        public ICommand CompletedCommand { get; private set; }
        public ICommand CanceledCommand { get; private set; }

        public HistoryViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            AllRequests = new ObservableCollection<RequestData>();
            FilteredRequests = new ObservableCollection<RequestData>();

            // Initialize commands
            RefreshCommand = new Command(async () => await LoadRequestsAsync());
            CompletedCommand = new Command(ShowCompletedRequests);
            //CanceledCommand = new Command(ShowCanceledRequests);
        }

        public async Task LoadRequestsAsync()
        {
            try
            {
                IsBusy = true;
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);
                OnPropertyChanged(nameof(IsBusy));

                // Fetch all completed requests
                var allRequests = await _firebaseClient.Child("complete").OnceAsync<RequestData>();

                AllRequests.Clear();
                TotalAmount = 0;

                // Iterate through each request
                foreach (var request in allRequests)
                {
                    if (request.Object.DriverId == mobileNumber)
                    {
                        // Get the service provider's name using the DriverId (mobile number)
                        var users = await _firebaseClient
                                                    .Child("users")
                                                    .OnceAsync<Users>();

                        var user = users.FirstOrDefault(v => v.Object.MobileNumber == request.Object.ServiceProviderId)?.Object;
                        if (user != null)
                        {
                            // Store the service provider's name in the request data
                            request.Object.ServiceProviderName = user.FullName;
                        }

                        // Add the request to the collection and update the total amount
                        AllRequests.Add(request.Object);
                        TotalAmount += request.Object.Price;
                    }
                }

                // Show completed requests by default
                ShowCompletedRequests();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load requests", "OK");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsBusy));
            }
        }


        private void ShowCompletedRequests()
        {
            FilteredRequests.Clear();
            foreach (var request in AllRequests.Where(r => r.Status == "Completed")) // Change "Completed" to your actual status
            {
                FilteredRequests.Add(request);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
