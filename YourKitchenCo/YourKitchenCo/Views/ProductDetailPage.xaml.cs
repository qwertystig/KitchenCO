using Microsoft.Maui.Controls;
using YourKitchenCo.Services;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Views;

public partial class ProductDetailPage : ContentPage
{
    private readonly ISessionService _session;

    public ProductDetailPage(ProductDetailViewModel viewModel, ISessionService session)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = PageAnimation.EntranceAsync(Content);

        // One-time-per-session reminder about the 9 AM order cutoff — shown
        // the first time the person looks at a product, before they've
        // picked a delivery date, so it's useful rather than an interruption.
        if (!_session.HasSeenCutoffNotice)
        {
            _session.HasSeenCutoffNotice = true;

            await AlertService.Instance.ShowAsync(
                "Order cutoff: 9:00 AM",
                "Orders must be placed by 9:00 AM to make the earliest available delivery slot. Anything placed after 9:00 AM rolls over to the next cutoff window.",
                "Got it");
        }
    }

    // The OnOptionCheckedChanged method has been removed completely 
    // because data bindings now handle state changes automatically.
}
