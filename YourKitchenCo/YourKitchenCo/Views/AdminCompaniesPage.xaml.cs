using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class AdminCompaniesPage : ContentPage
{
    public AdminCompaniesPage(AdminCompaniesViewModel viewModel)
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
