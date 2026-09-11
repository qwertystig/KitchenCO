using System;
using Microsoft.Maui.Controls;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class AdminDashboardPage : ContentPage
{
    private readonly AdminDashboardViewModel _viewModel;

    public AdminDashboardPage(AdminDashboardViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAnimation.EntranceAsync(Content);
        _viewModel?.RefreshOrdersCommand.Execute(null);
    }
}