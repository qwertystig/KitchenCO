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
}
