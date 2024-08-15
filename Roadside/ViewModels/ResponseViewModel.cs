using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using Roadside.Views;


namespace Roadside.ViewModels
{
    public class ResponseViewModel : BindableObject
    {
        private readonly FirebaseClient _firebaseClient;

        public ResponseViewModel(string key)
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            CheckAndDeletePendingRecord(key);
        }

        private async Task CheckAndDeletePendingRecord(string key)
        {
            try
            {
                // Wait for 1 minute before checking the status
                await Task.Delay(TimeSpan.FromMinutes(1));

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

                    await Application.Current.MainPage.DisplayAlert("Info", "Service Provider did not respond", "OK");
                    await App.Current.MainPage.Navigation.PushAsync(new HomePage());
                    



                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                await Application.Current.MainPage.DisplayAlert("Error", "Something went wrong: " + ex.Message, "OK");
            }
        }
    }
}
