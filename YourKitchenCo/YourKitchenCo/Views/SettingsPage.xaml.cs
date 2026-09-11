using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAnimation.EntranceAsync(Content);

        // This page is shared between AppShell (customer) and AdminShell —
        // only show the admin nav strip when actually reached via admin.
        AdminNav.IsVisible = Shell.Current is AdminShell;
    }
}