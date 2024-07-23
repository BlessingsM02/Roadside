using Roadside.Services;

namespace Roadside
{
    public partial class MainPage : ContentPage
    {
        private readonly IAuthenticationService _authenticationService;

        public MainPage(IAuthenticationService authenticationService)
        {
            InitializeComponent();
            CheckAuthenticationAsync();
            _authenticationService = authenticationService;
        }
        private async Task CheckAuthenticationAsync()
        {
            var authToken = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(authToken))
            {
                // Navigate to the next page if authenticated
                await Navigation.PushAsync(new Views.NewPage1());
            }
        }

        private async void Submit_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MobileEntry.Text))
            {
                var isValidMobile = await _authenticationService.AuthenticateMobile(MobileEntry.Text);
                if (isValidMobile)
                {
                    // Show verification UI and disable input controls
                    verificationInfo.IsVisible = true;
                    MobileEntry.IsEnabled = false;
                    btnS.IsEnabled = false;
                }
                else
                {
                    await DisplayAlert("Error", "Invalid Mobile Number", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a Mobile Number", "OK");
            }
        }

        private async void btnVerify_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(codeEntry.Text))
            {
                var isValidCode = await _authenticationService.ValidateOTP(codeEntry.Text);
                if (isValidCode)
                {
                    // Navigate to the next page upon successful validation
                    await Navigation.PushAsync(new Views.NewPage1());
                }
                else
                {
                    await DisplayAlert("Error", "Invalid Verification Code", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a Verification Code", "OK");
            }
        }
    }
}
