using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Roadside.ViewModels;

namespace Roadside.Views
{
    public partial class RequestDetailsPage : ContentPage
    {
        private RequestDetailsViewModel _viewModel;

        public RequestDetailsPage(string requestId)
        {
            InitializeComponent();
            BindingContext = _viewModel = new RequestDetailsViewModel(requestId);

            // Subscribe to ViewModel's location updates
            _viewModel.UserLocationUpdated += OnUserLocationUpdated;
            _viewModel.ServiceProviderLocationUpdated += OnServiceProviderLocationUpdated;
        }

        private void OnUserLocationUpdated(object sender, (Location location, string mobileNumber) data)
        {
            UpdatePin("You", data.location, data.mobileNumber);
        }

        private void OnServiceProviderLocationUpdated(object sender, (Location location, string mobileNumber) data)
        {
            UpdatePin("Service Provider", data.location, data.mobileNumber);
        }

        private void UpdatePin(string label, Location location, string mobileNumber)
        {
            var pin = ServiceMap.Pins.FirstOrDefault(p => p.Label.StartsWith(label));
            if (pin != null)
            {
                // Update existing pin location
                pin.Location = location;
            }
            else
            {
                // Add new pin
                pin = new Pin
                {
                    Label = $"{label} ({mobileNumber})",
                    Address = $"{label}'s location",
                    Type = PinType.Place,
                    Location = location
                };
                ServiceMap.Pins.Add(pin);
            }

            // Optionally, move the map's focus to the updated location
            ServiceMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(100)));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Stop updating locations when the page is closed
            _viewModel.StopUpdating();
        }
    }
}
