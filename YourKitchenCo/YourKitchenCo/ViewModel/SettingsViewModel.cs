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

    // Plain string, not an enum — matches this project's existing "mode"
    // convention (see StringEqualsConverter / MultiValueEqualsConverter) so
    // the 4 theme swatches in SettingsPage.xaml can highlight the selected
    // one with a DataTrigger, and so it round-trips through Preferences
    // (BrandThemeService) with no extra parsing.
    [ObservableProperty]
    private string _selectedBrandTheme = BrandThemeService.Current;

    public SettingsViewModel(ISessionService session)
    {
        _session = session;
        AccountEmail = session.CurrentUser?.Email ?? string.Empty;

        // Real, persisted device preferences — not just UI-only toggles.
        IsNotificationsEnabled = Preferences.Default.Get(NotificationsPrefKey, true);
        IsDarkModeEnabled = Preferences.Default.Get(DarkModePrefKey, Application.Current?.RequestedTheme == AppTheme.Dark);
        SelectedBrandTheme = BrandThemeService.SavedTheme;

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

    /// <summary>Admin previewing the customer app — Settings shows a "Back to Admin" card.</summary>
    public bool IsAdminPreview => AdminCustomerSwitch.IsAdminSession(_session) && Shell.Current is not AdminShell;

    [RelayCommand]
    private void ReturnToAdmin() => AdminCustomerSwitch.ReturnToAdmin();

    [RelayCommand]
    private async Task SelectBrandThemeAsync(string theme)
    {
        if (string.IsNullOrEmpty(theme) || theme == SelectedBrandTheme) return;

        // {StaticResource}/{AppThemeBinding} colours only resolve once, when
        // a page is built — unlike the dark-mode toggle above, applying a
        // new brand theme can't just flip a live binding. The whole visible
        // Shell has to be rebuilt so every page re-resolves its colours
        // against the new palette, which briefly resets navigation back to
        // the shell's default tab — hence the heads-up before doing it.
        bool confirm = await AlertService.Instance.ShowConfirmAsync(
            "Change App Theme",
            "This refreshes the app to apply the new colour theme, then brings you back here.",
            "Apply", "Cancel");
        if (!confirm) return;

        SelectedBrandTheme = theme;
        BrandThemeService.Apply(theme);

        bool isAdmin = Shell.Current is AdminShell;
        Application.Current!.MainPage = isAdmin ? new AdminShell() : new AppShell();

        try
        {
            await Shell.Current.GoToAsync(isAdmin ? "//adminsettings" : nameof(SettingsPage));
        }
        catch
        {
            // Non-fatal — the new theme is already applied and persisted
            // either way; worst case they land on the default tab instead
            // of back here on Settings.
        }
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
