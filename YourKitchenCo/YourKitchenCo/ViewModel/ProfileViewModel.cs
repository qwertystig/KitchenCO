using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YourKitchenCo.Services;
using YourKitchenCo.Views;

namespace YourKitchenCo.ViewModel;

public partial class ProfileViewModel : ObservableObject
{
    private readonly ISessionService _session;
    private readonly ICompanyDirectoryService _companyDirectory;
    private readonly IUserDirectoryService _userDirectory;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _role = string.Empty;

    [ObservableProperty]
    private string _companyName = "—";

    [ObservableProperty]
    private string _mealSubsidyLabel = string.Empty;

    [ObservableProperty]
    private string _locationName = "—";

    [ObservableProperty]
    private string _deliveryFloor = "Not set";

    [ObservableProperty]
    private string _memberSince = string.Empty;

    public ProfileViewModel(ISessionService session, ICompanyDirectoryService companyDirectory, IUserDirectoryService userDirectory)
    {
        _session = session;
        _companyDirectory = companyDirectory;
        _userDirectory = userDirectory;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var user = _session.CurrentUser;
        if (user is null) return;

        UserName = user.FullName;
        Email = user.Email;
        Role = user.Role;
        MemberSince = user.JoinedDate.ToString("MMMM yyyy");
        DeliveryFloor = string.IsNullOrWhiteSpace(user.DeliveryFloor) ? "Not set" : user.DeliveryFloor;

        if (!string.IsNullOrWhiteSpace(user.CompanyId))
        {
            var company = await _companyDirectory.GetCompanyAsync(user.CompanyId);
            CompanyName = company?.Name ?? "—";
            MealSubsidyLabel = company is not null
                ? $"R{company.MealSubsidyAmount:F2} per meal, incl. VAT"
                : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(user.LocationId))
        {
            var location = await _companyDirectory.GetLocationAsync(user.LocationId);
            LocationName = location?.Name ?? "—";
        }
    }

    [RelayCommand]
    private async Task ChangeProfilePicture()
    {
        if (Application.Current?.MainPage != null)
        {
            await AlertService.Instance.ShowAsync("Coming Soon", "Profile photo uploads aren't wired up yet.", "OK");
        }
    }

    [RelayCommand]
    private async Task EditDeliveryFloorAsync()
    {
        var user = _session.CurrentUser;
        if (user is null) return;

        string newFloor = await AlertService.Instance.ShowPromptAsync(
            "Delivery Floor",
            "Which floor or pantry drop-off point should your order be delivered to?",
            initialValue: user.DeliveryFloor);

        if (newFloor is null) return; // cancelled

        user.DeliveryFloor = newFloor;
        await _userDirectory.UpdateUserAsync(user);
        DeliveryFloor = string.IsNullOrWhiteSpace(newFloor) ? "Not set" : newFloor;
    }

    [RelayCommand]
    private async Task NavigateToSettingsAsync() => await Shell.Current.GoToAsync(nameof(Views.SettingsPage));

    [RelayCommand]
    private async Task NavigateToHelpAsync() => await Shell.Current.GoToAsync(nameof(Views.HelpPage));

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
