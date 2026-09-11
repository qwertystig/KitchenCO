using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IUserDirectoryService _userDirectory;
    private readonly ICompanyDirectoryService _companyDirectory;
    private readonly ISessionService _session;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isCompanyAutoMatched;

    partial void OnEmailChanged(string value)
    {
        // Auto-match to a company by email domain, matching the reference
        // app's domain-whitelisting — a convenience, not a requirement.
        // Doesn't override a choice the person already made manually;
        // only fills it in while the company picker is still empty.
        if (SelectedCompany is not null || string.IsNullOrWhiteSpace(value)) return;

        var atIndex = value.LastIndexOf('@');
        if (atIndex < 0 || atIndex == value.Length - 1) return;

        var domain = value[(atIndex + 1)..].Trim().ToLowerInvariant();
        var match = Companies.FirstOrDefault(c => c.WhitelistedDomains.Contains(domain));

        if (match is not null)
        {
            SelectedCompany = match;
            IsCompanyAutoMatched = true;
        }
    }

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private Company? _selectedCompany;

    [ObservableProperty]
    private CompanyLocation? _selectedLocation;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<Company> Companies { get; } = new();
    public ObservableCollection<CompanyLocation> Locations { get; } = new();

    public RegisterViewModel(IUserDirectoryService userDirectory, ICompanyDirectoryService companyDirectory, ISessionService session)
    {
        _userDirectory = userDirectory;
        _companyDirectory = companyDirectory;
        _session = session;
        _ = LoadCompaniesAsync();
    }

    private async Task LoadCompaniesAsync()
    {
        Companies.Clear();
        foreach (var company in await _companyDirectory.GetCompaniesAsync())
            Companies.Add(company);
    }

    partial void OnSelectedCompanyChanged(Company? value) => _ = LoadLocationsAsync(value);

    private async Task LoadLocationsAsync(Company? company)
    {
        Locations.Clear();
        SelectedLocation = null;

        if (company is null) return;

        foreach (var location in await _companyDirectory.GetLocationsAsync(company.Id))
            Locations.Add(location);
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (Application.Current?.MainPage == null) return;

        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
        {
            await AlertService.Instance.ShowAsync("Missing Information", "Please enter your name and email address.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
        {
            await AlertService.Instance.ShowAsync("Weak Password", "Password must be at least 8 characters.", "OK");
            return;
        }

        if (Password != ConfirmPassword)
        {
            await AlertService.Instance.ShowAsync("Passwords Don't Match", "Please make sure both password fields match.", "OK");
            return;
        }

        if (SelectedCompany is null || SelectedLocation is null)
        {
            await AlertService.Instance.ShowAsync("Company Required", "Please select your company and delivery location — orders are grouped by these.", "OK");
            return;
        }

        IsBusy = true;

        var existing = await _userDirectory.GetByEmailAsync(Email);
        if (existing is not null)
        {
            IsBusy = false;
            await AlertService.Instance.ShowAsync("Account Exists", "An account with that email already exists. Try logging in instead.", "OK");
            return;
        }

        // Password is now genuinely stored and checked at login — see
        // UserAccount.Password for why plain text is acceptable only in
        // this mock/demo context.
        var newUser = new UserAccount
        {
            FullName = FullName,
            Email = Email,
            Password = Password,
            Role = "Customer",
            CompanyId = SelectedCompany.Id,
            LocationId = SelectedLocation.Id
        };

        await _userDirectory.AddUserAsync(newUser);
        _session.SignIn(newUser);

        IsBusy = false;

        await AlertService.Instance.ShowAsync("Welcome!", $"Account created for {newUser.FullName}. You're all set.", "OK");

        var services = Application.Current!.Handler?.MauiContext?.Services;
        if (services != null)
        {
            var selectDayPage = services.GetRequiredService<Views.SelectDeliveryDayPage>();
            Application.Current.MainPage = new NavigationPage(selectDayPage);
        }
        else
        {
            Application.Current.MainPage = new AppShell(); // fallback if services aren't reachable yet
        }
    }

    [RelayCommand]
    private async Task BackToLoginAsync()
    {
        if (Application.Current?.MainPage?.Navigation != null)
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}
