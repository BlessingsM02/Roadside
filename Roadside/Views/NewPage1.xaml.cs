using Roadside.ViewModels;
namespace Roadside.Views;


public partial class NewPage1 : ContentPage
{
	public NewPage1()
	{
		InitializeComponent();
		BindingContext = new UserViewModel();
	}
}