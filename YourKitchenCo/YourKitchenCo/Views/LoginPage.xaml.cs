using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourKitchenCo.Views;

public partial class LoginPage : ContentPage
{
    // Updated to use the singular ViewModel namespace
    public LoginPage(ViewModel.LoginViewModel viewModel)
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