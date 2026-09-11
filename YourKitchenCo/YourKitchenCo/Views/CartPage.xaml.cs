using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.Maui.Controls;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class CartPage : ContentPage
{
    public CartPage(CartPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAnimation.EntranceAsync(Content);

        // FIXED: Force the view model to re-notify the UI layout engine every time the page appears
        if (BindingContext is CartPageViewModel vm)
        {
            vm.Refresh();
        }
    }
}