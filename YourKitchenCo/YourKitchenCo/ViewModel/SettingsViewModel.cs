using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using YourKitchenCo.Services;
using YourKitchenCo.Views;

namespace YourKitchenCo.ViewModel;

public partial class SettingsViewModel : ObservableObject
{
    private const string NotificationsPrefKey = "settings.notifications_enabled";
    private const string DarkModePrefKey = "settings.dark_mode_enabled";

    private readonly ISessionService _session;
    private bool _isLoading = true; // guards against writing prefs back to themselves while loading

    [ObservableProperty]
    private string _accountEmail = string.Empty;

    [ObservableProperty]
    private bool _isNotificationsEnabled;

    [ObservableProperty]
    private bool _isDarkModeEnabled;

    public SettingsViewModel(ISessionService session)
    {
        _session = session;
        AccountEmail = session.CurrentUser?.Email ?? string.Empty;

        // Real, persisted device preferences — not just UI-only toggles.
        IsNotificationsEnabled = Preferences.Default.Get(NotificationsPrefKey, true);
        IsDarkModeEnabled = Preferences.Default.Get(DarkModePrefKey, Application.Current?.RequestedTheme == AppTheme.Dark);

        _isLoading = false;
    }

    partial void OnIsNotificationsEnabledChanged(bool value)
    {
        if (_isLoading) return;
        Preferences.Default.Set(NotificationsPrefKey, value);
        // No push notification backend exists yet (see CHANGELOG) — this just
        // records the person's preference for whenever that's wired up.
    }

    partial void OnIsDarkModeEnabledChanged(bool value)
    {
        if (_isLoading) return;
        Preferences.Default.Set(DarkModePrefKey, value);

        if (Application.Current is not null)
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        if (Application.Current?.MainPage == null) return;

        bool confirm = await AlertService.Instance.ShowConfirmAsync("Sign Out", "Are you sure you want to sign out?", "Sign Out", "Cancel");
        if (!confirm) return;

        _session.SignOut();

        var services = Shell.Current?.Handler?.MauiContext?.Services;
        if (services == null) return;

        var loginPage = services.GetRequiredService<LoginPage>();
        Application.Current!.MainPage = new NavigationPage(loginPage);
    }
}
