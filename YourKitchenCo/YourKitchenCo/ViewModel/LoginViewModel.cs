using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;
using YourKitchenCo.Views;

namespace YourKitchenCo.ViewModel;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUserDirectoryService _userDirectory;
    private readonly ISessionService _session;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isRememberMe;

    public LoginViewModel(IUserDirectoryService userDirectory, ISessionService session)
    {
        _userDirectory = userDirectory;
        _session = session;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var mainPage = Application.Current?.MainPage;
        if (mainPage?.Navigation == null) return;

        // Resolved through DI so RegisterViewModel gets its required services
        // (IUserDirectoryService/ICompanyDirectoryService/ISessionService) —
        // there's no safe parameterless fallback for it.
        var services = Application.Current?.Handler?.MauiContext?.Services
                        ?? mainPage.Handler?.MauiContext?.Services;
        if (services == null) return;

        var registerPage = services.GetRequiredService<RegisterPage>();
        await mainPage.Navigation.PushAsync(registerPage);
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        if (Application.Current?.MainPage != null)
        {
            await AlertService.Instance.ShowAsync("Help", "Forgot password logic coming soon!", "OK");
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (Application.Current == null) return;

        if (string.IsNullOrWhiteSpace(this.Email))
        {
            if (Application.Current.MainPage != null)
            {
                await AlertService.Instance.ShowAsync("Login Error", "Please enter a valid email address.", "OK");
            }
            return;
        }

        // Look the email up in the (mock, for now) user directory. Falls
        // back to an ad-hoc guest customer account so the demo doesn't
        // dead-end on an unrecognised email.
        var user = await _userDirectory.GetByEmailAsync(this.Email);

        if (user is null)
        {
            bool looksLikeAdmin = this.Email.Contains("admin", StringComparison.OrdinalIgnoreCase);

            user = new UserAccount
            {
                FullName = this.Email,
                Email = this.Email,
                Role = looksLikeAdmin ? "Admin" : "Customer"
            };
        }

        // Real password check, but only for accounts that actually have one
        // set — every seeded demo account and the ad-hoc guest above has an
        // empty Password, so they keep accepting any password exactly as
        // before. Only accounts created through registration (which now
        // genuinely captures a password) get checked for real.
        if (!string.IsNullOrEmpty(user.Password) && user.Password != this.Password)
        {
            await AlertService.Instance.ShowAsync("Login Error", "Incorrect password. Please try again.", "OK");
            return;
        }

        if (!user.IsActive)
        {
            await AlertService.Instance.ShowAsync("Account Suspended", "This account has been suspended. Please contact your admin.", "OK");
            return;
        }

        _session.SignIn(user);

        if (user.Role == "Admin")
        {
            Application.Current.MainPage = new AdminShell();
            return;
        }

        // Customers pick which day they're ordering for before anything else —
        // that choice becomes the default delivery date for their basket.
        var services = Application.Current.Handler?.MauiContext?.Services;
        if (services != null)
        {
            var selectDayPage = services.GetRequiredService<SelectDeliveryDayPage>();
            Application.Current.MainPage = new NavigationPage(selectDayPage);
        }
        else
        {
            Application.Current.MainPage = new AppShell(); // fallback if services aren't reachable yet
        }
    }
}
