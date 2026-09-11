using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class OrderConfirmationPage : ContentPage
{
    public OrderConfirmationPage(OrderConfirmationViewModel viewModel)
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
