using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.Views;

public partial class AdminNavStrip : ContentView
{
    public static readonly BindableProperty ActiveRouteProperty =
        BindableProperty.Create(nameof(ActiveRoute), typeof(string), typeof(AdminNavStrip), string.Empty);

    /// <summary>
    /// The route of the page currently hosting this strip — e.g.
    /// "adminreports". Each host page sets this to its own route so the
    /// matching tab highlights. Set once per page, not automatically
    /// derived, since a ContentView embedded in a page has no reliable way
    /// to know its own host's route from the inside.
    /// </summary>
    public string ActiveRoute
    {
        get => (string)GetValue(ActiveRouteProperty);
        set => SetValue(ActiveRouteProperty, value);
    }

    public AdminNavStrip()
    {
        InitializeComponent();

        // Live clock, matching what the old flyout header showed. Uses the
        // device's local time rather than hardcoding a SAST (UTC+2) offset
        // the way the reference app does — safer, since assuming every
        // deployment runs in South African time would be wrong the moment
        // that's not true.
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => ClockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        timer.Start();
        ClockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    private async void OnOverviewClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//admindashboard");
    private async void OnActiveOrdersClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//adminactiveorders");
    private async void OnCompaniesClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//admincompanies");
    private async void OnDiscountsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//admindiscounts");
    private async void OnNotificationsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//adminnotifications");
    private async void OnMenuCatalogClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//adminmenu");
    private async void OnUserManagementClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//adminusers");
    private async void OnReportsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//adminreports");
    private async void OnSettingsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//adminsettings");

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        if (Application.Current == null) return;

        var services = Handler?.MauiContext?.Services;
        if (services == null) return; // control isn't attached to a handler yet — nothing safe to do

        services.GetService<Services.ISessionService>()?.SignOut();

        var loginPage = services.GetRequiredService<LoginPage>();
        Application.Current.MainPage = new NavigationPage(loginPage);
    }
}
