using System.Threading.Tasks;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class SelectDeliveryDayPage : ContentPage
{
    public SelectDeliveryDayPage(SelectDeliveryDayViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAnimation.EntranceAsync(Content);
    }

    /// <summary>Close ("✕") tapped — only visible/reachable in the popup (mid-session) presentation.</summary>
    private async void OnCloseClicked(object? sender, EventArgs e) => await CancelAsync();

    /// <summary>
    /// Hardware/gesture back while shown as a popup should dismiss it the
    /// same way the ✕ does, rather than falling through to whatever the
    /// platform default for a modal page happens to be.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is SelectDeliveryDayViewModel { IsChangingDay: true })
        {
            _ = CancelAsync();
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private async Task CancelAsync()
    {
        // Nothing is committed until Confirm is tapped (ConfirmCommand is
        // what writes SelectedOrderingDate), so backing out here is safe —
        // just close the popup and leave the previous selection untouched.
        await Navigation.PopModalAsync();
    }
}
