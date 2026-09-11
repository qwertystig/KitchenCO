using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class OrderHistoryPage : ContentPage
{
    public OrderHistoryPage(OrderHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subtle Page Entrance Animation
        if (Content != null)
        {
            Content.Opacity = 0;
            Content.TranslationY = 15;

            await Task.WhenAll(
                Content.FadeTo(1, 250, Easing.CubicOut),
                Content.TranslateTo(0, 0, 250, Easing.CubicOut)
            );
        }
    }
}