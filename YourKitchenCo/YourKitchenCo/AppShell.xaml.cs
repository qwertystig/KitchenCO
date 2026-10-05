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

    /// <summary>
    /// Tapping a bottom tab should land on that tab's root screen and close
    /// whatever was open before — not resurrect a product page / cart /
    /// settings page that was pushed on some other tab the last time it was
    /// visited (Shell's default is to keep each tab's stack exactly as it was
    /// left). So whenever the selected tab changes, every *other* tab's stack
    /// is popped back to its root. Runs after the switch has happened, so if
    /// a pop can't be applied for any reason the tab change itself is
    /// unaffected — this only ever tidies up, never blocks navigation.
    /// </summary>
    protected override async void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        if (args.Source != ShellNavigationSource.ShellSectionChanged) return;

        var tabBar = CurrentItem;
        var activeTab = tabBar?.CurrentItem;
        if (tabBar is null || activeTab is null) return;

        foreach (var tab in tabBar.Items)
        {
            if (ReferenceEquals(tab, activeTab)) continue;

            try
            {
                if (tab.Navigation.NavigationStack.Count > 1)
                    await tab.Navigation.PopToRootAsync(animated: false);
            }
            catch
            {
                // Best effort — leave that tab's stack as-is rather than
                // interrupt the navigation that just completed.
            }
        }
    }
}
