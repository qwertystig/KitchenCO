using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class AdminActiveOrdersPage : ContentPage
{
    public AdminActiveOrdersPage(AdminActiveOrdersViewModel viewModel)
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
