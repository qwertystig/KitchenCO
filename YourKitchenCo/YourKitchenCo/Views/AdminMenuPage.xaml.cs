using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class AdminMenuPage : ContentPage
{
    public AdminMenuPage(AdminMenuViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Shared entrance animation (PageAnimation.cs) — was its own
        // hand-rolled copy (250ms) slightly out of step with the 260ms every
        // other page uses; now consistent with the rest of the app.
        await PageAnimation.EntranceAsync(Content);
    }
}