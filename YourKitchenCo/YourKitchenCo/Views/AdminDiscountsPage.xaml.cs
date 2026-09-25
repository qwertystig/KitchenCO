using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class AdminDiscountsPage : ContentPage
{
    public AdminDiscountsPage(AdminDiscountsViewModel viewModel)
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
