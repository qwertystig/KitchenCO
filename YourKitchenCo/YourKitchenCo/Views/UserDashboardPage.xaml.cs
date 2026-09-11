using Microsoft.Maui.Controls;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class UserDashboardPage : ContentPage
{
    private readonly UserDashboardViewModel _viewModel;

    public UserDashboardPage(UserDashboardViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAnimation.EntranceAsync(Content);
        if (_viewModel != null)
        {
            await _viewModel.LoadDataAsync();
        }
    }
}