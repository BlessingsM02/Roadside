using Roadside.Services;
using Roadside.Views;

namespace Roadside
{
    public partial class MainPage : ContentPage
    {
        private readonly IAuthenticationService _authenticationService;
        private bool _isOTPPhase = false; // To track whether we are in the OTP phase

        public MainPage(IAuthenticationService authenticationService)
        {
            InitializeComponent();
            _authenticationService = authenticationService;

            CheckIfUserIsLoggedIn();
        }

        private void CheckIfUserIsLoggedIn()
        {
            var savedMobileNumber = Preferences.Get("mobile_number", string.Empty);
            if (!string.IsNullOrEmpty(savedMobileNumber))
            {
                Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
        }

        private async void Submit_Clicked(object sender, EventArgs e)
        {
            if (_isOTPPhase)
            {
                await VerifyOTP();
            }
            else
            {
                await SubmitMobileNumber();
            }
        }

        private async Task SubmitMobileNumber()
        {
            if (IsValidMobileNumber())
            {
                var isValidMobile = await _authenticationService.AuthenticateMobile("+26"+MobileEntry.Text);
                if (isValidMobile)
                {
                    TransitionToOTPPhase();
                }
                else
                {
                    await DisplayAlert("Error", "Enter a valid mobile number", "OK");
                }
            }
        }

        private async Task VerifyOTP()
        {
            if (IsValidOTP())
            {
                var isValidCode = await _authenticationService.ValidateOTP(codeEntry.Text);
                if (isValidCode)
                {
                    await Navigation.PushAsync(new NewPage1());
                }
                else
                {
                    await DisplayAlert("Error", "Invalid Verification Code", "OK");
                }
            }
        }

        private void TransitionToOTPPhase()
        {
            _isOTPPhase = true;
            MobileEntry.IsEnabled = false; // Disable mobile number input
            codeEntry.IsEnabled = true;    // Enable OTP input
            btnSubmit.Text = "Verify";     // Change button text to "Verify"
        }

        private bool IsValidMobileNumber()
        {
            if (string.IsNullOrWhiteSpace(MobileEntry.Text))
            {
                DisplayAlert("Error", "Please enter a Mobile Number", "OK");
                return false;
            }
            return true;
        }

        private bool IsValidOTP()
        {
            if (string.IsNullOrWhiteSpace(codeEntry.Text))
            {
                DisplayAlert("Error", "Please enter a Verification Code", "OK");
                return false;
            }
            return true;
        }
    }
}
