using Mopups.Pages;
using Mopups.Services;

namespace Roadside.Views;

public partial class RatingPopup : PopupPage
{
    private TaskCompletionSource<int?> _ratingCompletionSource;

    public RatingPopup()
    {
        InitializeComponent();
        _ratingCompletionSource = new TaskCompletionSource<int?>();
        BindingContext = this;
    }

    public Task<int?> GetRatingAsync() => _ratingCompletionSource.Task;

    public Command SubmitCommand => new Command(() =>
    {
        if (RatingPicker.SelectedItem != null && int.TryParse(RatingPicker.SelectedItem.ToString(), out int selectedRating))
        {
            _ratingCompletionSource.TrySetResult(selectedRating);
            ClosePopup();
        }
        else
        {
            Application.Current.MainPage.DisplayAlert("Error", "Please select a valid rating.", "OK");
        }
    });

    private async void ClosePopup()
    {
        await MopupService.Instance.PopAsync();
    }
}
