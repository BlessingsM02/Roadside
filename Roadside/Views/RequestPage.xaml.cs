using Roadside.ViewModels;

namespace Roadside.Views;

public partial class RequestPage : ContentPage
{
	public RequestPage()
	{
		InitializeComponent();
		BindingContext = new RequestViewModel();
	}
}