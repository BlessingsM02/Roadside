using Roadside.ViewModels;

namespace Roadside.Views;

public partial class LoadingPage : ContentPage
{
	public LoadingPage()
	{
		InitializeComponent();
		BindingContext = new LoadingViewModel();
    }

   
}