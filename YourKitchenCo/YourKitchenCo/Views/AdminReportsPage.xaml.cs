using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class AdminReportsPage : ContentPage
{
    public AdminReportsPage(AdminReportsViewModel viewModel)
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