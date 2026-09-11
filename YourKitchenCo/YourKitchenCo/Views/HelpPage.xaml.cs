using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class HelpPage : ContentPage
{
    public HelpPage(HelpViewModel viewModel)
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
