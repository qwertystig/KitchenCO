using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class PaymentPage : ContentPage
{
    public PaymentPage(PaymentViewModel viewModel)
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
