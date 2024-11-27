using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using Mopups.Pages;
using Mopups.Services;
using System.Windows.Input;

namespace Roadside.Views;

public partial class TowTruckDetailsPopup : PopupPage
{
    public TowTruckDetailsPopup(string name, string address, string contact)
    {
        InitializeComponent();
        CompanyName.Text = name;
        CompanyAddress.Text = address;
        CompanyContact.Text = contact;

        // Bind commands
        CloseCommand = new Command(async () => await MopupService.Instance.PopAsync());
        DialCommand = new Command<string>(DialNumber);
        BindingContext = this;
    }

    public ICommand CloseCommand { get; }
    public ICommand DialCommand { get; }

    private void DialNumber(string phoneNumber)
    {
        try
        {
            if (PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open(phoneNumber);
            else
                App.Current.MainPage.DisplayAlert("Error", "Dialer not supported on this device.", "OK");
        }
        catch (Exception ex)
        {
            App.Current.MainPage.DisplayAlert("Error", $"Failed to open dialer", "OK");
        }
    }
}
