using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Services;

/// <summary>
/// Lets an admin flip between the admin UI (AdminShell) and the customer UI
/// (AppShell) in the same signed-in session, to preview their changes as a
/// client would see them. Both directions just swap MainPage — the same
/// thing LoginViewModel does after sign-in — so nothing about the session
/// or the DI-held services changes.
/// </summary>
public static class AdminCustomerSwitch
{
    public static bool IsAdminSession(ISessionService session) =>
        string.Equals(session.CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase);

    /// <summary>Admin → customer app. Ensures a default ordering day exists so the dashboard has a date to show.</summary>
    public static void EnterCustomerView(IServiceProvider services)
    {
        var session = services.GetRequiredService<ISessionService>();
        if (session.SelectedOrderingDate is null)
        {
            var dates = services.GetService<IOrderSchedulingService>()?.GetOrderableDeliveryDates(10);
            if (dates is { Count: > 0 })
                session.SelectedOrderingDate = dates[0];
        }

        if (Application.Current is not null)
            Application.Current.MainPage = new AppShell();
    }

    /// <summary>Customer app → admin dashboard.</summary>
    public static void ReturnToAdmin()
    {
        if (Application.Current is not null)
            Application.Current.MainPage = new AdminShell();
    }
}
