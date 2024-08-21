using Roadside.ViewModels;

namespace Roadside.Views
{
    public partial class ResponsePage : ContentPage
    {
        public ResponsePage(string key)
        {
            InitializeComponent();
            BindingContext = new ResponseViewModel();
        }
    }
}
