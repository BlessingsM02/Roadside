using Mopups.Services;
using Roadside.ViewModels;
using System.Windows.Input;

namespace Roadside.Views;

public partial class RequestDetailsBottomSheet
{
    private readonly RequestDetailsViewModel _viewModel;


    public ICommand CloseCommand { get; }

    public RequestDetailsBottomSheet()
    {
        InitializeComponent();

        // Command to close the popup
        _viewModel = new RequestDetailsViewModel();
        BindingContext = _viewModel; // Prevent closing when tapping outside the popup
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadRequestDetailsCommand.Execute(null);
    }
}