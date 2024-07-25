using Microsoft.Maui.Maps;
namespace Roadside.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();

    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromMicroseconds(20));
        var location = await Geolocation.GetLocationAsync(geolocationRequest);

        mat.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));
       

    }
    private async void requestButton_Clicked(object sender, EventArgs e)
    {



        try
        {
            var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(20));
            var location = await Geolocation.GetLocationAsync(geolocationRequest);

            // Clear existing pins on the map
            /* mat.Pins.Clear();

             // Add a new pin for the current location
             var pin = new Pin
             {
                 Address = $"{location}",
                 Location = location,
                 Type = PinType.Place,
                 Label = "Current",
             };
             mat.Pins.Add(pin);*/

            await Navigation.PushAsync(new RequestPage());


        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }
}