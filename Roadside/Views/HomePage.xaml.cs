using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Mopups.Services;
using System.Collections.Generic;

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

        try
        {
            var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(20));
            var location = await Geolocation.GetLocationAsync(geolocationRequest);

            mat.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));

            // Load pins for tow truck companies
            AddTowTruckCompanyPins();
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private void AddTowTruckCompanyPins()
    {
        var companies = new List<(string Name, string Address, string contact, double Latitude, double Longitude)>
{
            ("Roadside Rescue", "123 Church Rd, Lusaka", "+260977123456", -15.416667, 28.283333),
            ("Gladtidings", "456 Cairo Rd, Lusaka", "+260955654321", -15.419372, 28.281525),
            ("Friendy Bot", "789 Alick Nkhata Rd, Lusaka", "+260964789012", -15.406667, 28.323889)
};


        foreach (var company in companies)
        {
            var pin = new Pin
            {
                Label = company.Name,
                Address = company.Address,
                Location = new Location(company.Latitude, company.Longitude),
                Type = PinType.Place
            };

            pin.MarkerClicked += async (s, e) =>
            {
                e.HideInfoWindow = true; // Prevent default info window
                await ShowCompanyDetails(company.Name, company.Address,company.contact);
            };

            mat.Pins.Add(pin);
        }
    }

    private async Task ShowCompanyDetails(string name, string address, string contact)
    {
        var popup = new TowTruckDetailsPopup(name, address, contact);
        await MopupService.Instance.PushAsync(popup);
    }


    private async void requestButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            var geolocationRequest = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(20));
            var location = await Geolocation.GetLocationAsync(geolocationRequest);

            var requestBottomSheet = new RequestPage();
            await MopupService.Instance.PushAsync(requestBottomSheet);
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private void Map_MapClicked(object sender, MapClickedEventArgs e)
    {
        // Handle map click if needed
    }
}
