using Microsoft.Maui.Controls;

namespace YourKitchenCo;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Customer detail & modal navigation routes — pushed on top of
        // whichever bottom tab is active, not part of the TabBar itself.
        // Settings and Help used to be their own flyout entries; now that
        // there's no flyout, they're reached via Profile > Settings /
        // Profile > Help & Support instead, so they still need a route to
        // navigate to even though they're not in the visible tab structure.
        Routing.RegisterRoute(nameof(Views.RegisterPage), typeof(Views.RegisterPage));
        Routing.RegisterRoute(nameof(Views.ProductDetailPage), typeof(Views.ProductDetailPage));
        Routing.RegisterRoute(nameof(Views.CartPage), typeof(Views.CartPage));
        Routing.RegisterRoute(nameof(Views.PaymentPage), typeof(Views.PaymentPage));
        Routing.RegisterRoute(nameof(Views.TaxInvoicePage), typeof(Views.TaxInvoicePage));
        Routing.RegisterRoute(nameof(Views.OrderConfirmationPage), typeof(Views.OrderConfirmationPage));
        Routing.RegisterRoute(nameof(Views.CancellationPolicyPage), typeof(Views.CancellationPolicyPage));
        Routing.RegisterRoute(nameof(Views.SettingsPage), typeof(Views.SettingsPage));
        Routing.RegisterRoute(nameof(Views.HelpPage), typeof(Views.HelpPage));
    }
}
