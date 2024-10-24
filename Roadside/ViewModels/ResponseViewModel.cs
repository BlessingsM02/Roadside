using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using Roadside.Views;
using System.Threading;
using System.Threading.Tasks;

namespace Roadside.ViewModels
{
    public class ResponseViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;
        private CancellationTokenSource _cancellationTokenSource;

        public ResponseViewModel(string key)
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            _cancellationTokenSource = new CancellationTokenSource();

            // Start both tasks: checking for a pending record and periodically checking request table
            CheckAndDeletePendingRecord(key, _cancellationTokenSource.Token);
            PeriodicallyCheckRequestTable(key, _cancellationTokenSource.Token);
        }

        private async Task CheckAndDeletePendingRecord(string key, CancellationToken cancellationToken)
        {
            try
            {
                // Wait for 1 minute before checking the status
                await Task.Delay(TimeSpan.FromSeconds(40), cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;

                // Retrieve the record by key
                var record = await _firebaseClient
                    .Child("request")
                    .Child(key)
                    .OnceSingleAsync<dynamic>();

                // Check if the status is still "Pending"
                if (record != null && record.Status == "Pending")
                {
                    // Delete the record
                    await _firebaseClient
                        .Child("request")
                        .Child(key)
                        .DeleteAsync();

                    await Application.Current.MainPage.DisplayAlert("Info", "Service Provider did not respond in time.", "OK");
                    await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                }
            }
            catch (TaskCanceledException)
            {
                // Task was canceled, handle accordingly if necessary
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                await Application.Current.MainPage.DisplayAlert("Error", "Something went wrong: " + ex.Message, "OK");
            }
        }

        private async Task PeriodicallyCheckRequestTable(string key, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Check every 5 seconds
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;

                    // Query the "request" table to see if the user exists
                    var request = await _firebaseClient
                        .Child("request")
                        .Child(key)
                        .OnceSingleAsync<dynamic>();

                    if (request != null && request.Status == "Accepted")
                    {
                        // The request has been accepted
                        await Application.Current.MainPage.DisplayAlert("Success", "Your request has been accepted.", "OK");

                        // Stop further checks
                        _cancellationTokenSource.Cancel();

                        // Navigate to the RequestDetailsPage
                        await Shell.Current.GoToAsync($"//{nameof(RequestDetailsPage)}");
                    }

                    if (request != null && request.Status == "Declined")
                    {
                        // The request has been accepted
                        await Application.Current.MainPage.DisplayAlert("Info", "Your request has been Declined.", "OK");

                        // Stop further checks
                        _cancellationTokenSource.Cancel();

                        // Navigate to the RequestDetailsPage
                        await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
                    }

                }
            }
            catch (TaskCanceledException)
            {
                // Task was canceled
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred while checking the request table: " + ex.Message, "OK");
            }
        }

        // Method to stop both tasks when no longer needed
        public void StopChecking()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
