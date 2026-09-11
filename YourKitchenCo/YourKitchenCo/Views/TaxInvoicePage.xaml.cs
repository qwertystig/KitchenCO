using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class TaxInvoicePage : ContentPage
{
    public TaxInvoicePage(TaxInvoiceViewModel viewModel)
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
