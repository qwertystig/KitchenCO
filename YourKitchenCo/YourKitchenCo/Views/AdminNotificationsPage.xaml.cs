using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class AdminNotificationsPage : ContentPage
{
    public AdminNotificationsPage(AdminNotificationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAnimation.EntranceAsync(Content);
    }
}