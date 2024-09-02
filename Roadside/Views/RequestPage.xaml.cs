using Roadside.ViewModels;

namespace Roadside.Views;

public partial class RequestPage
{
	public RequestPage()
	{
		InitializeComponent();
		BindingContext = new RequestViewModel();
	}
}