using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class AdminUsersViewModel : ObservableObject
{
    private readonly IUserDirectoryService _userDirectory;
    private readonly ICompanyDirectoryService _companyDirectory;
    private List<UserAccount> _allUsers = new();

    public ObservableCollection<UserAccount> Users { get; } = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedRoleFilter = "All";

    public ObservableCollection<string> RoleFilterOptions { get; } = new()
    {
        "All",
        "Customer",
        "Kitchen Staff",
        "Admin"
    };

    public AdminUsersViewModel(IUserDirectoryService userDirectory, ICompanyDirectoryService companyDirectory)
    {
        _userDirectory = userDirectory;
        _companyDirectory = companyDirectory;
        _ = LoadUsersAsync();
    }

    /// <summary>
    /// Admin > Users > "+ Add User": name, email, role, then (for customers)
    /// company, location and floor/desk — the same fields self-registration
    /// collects, so the account is immediately usable for ordering.
    /// </summary>
    [RelayCommand]
    private async Task AddUserAsync()
    {
        var fullName = await AlertService.Instance.ShowPromptAsync("New User", "Full name:");
        if (string.IsNullOrWhiteSpace(fullName)) return;

        var email = await AlertService.Instance.ShowPromptAsync("New User", "Email address:", keyboard: Keyboard.Email);
        if (string.IsNullOrWhiteSpace(email)) return;
        email = email.Trim().ToLowerInvariant();
        if (!email.Contains('@'))
        {
            await AlertService.Instance.ShowAsync("Invalid Email", "Please enter a valid email address.", "OK");
            return;
        }
        if (await _userDirectory.GetByEmailAsync(email) is not null)
        {
            await AlertService.Instance.ShowAsync("Already Registered", $"{email} already has an account.", "OK");
            return;
        }

        var role = await AlertService.Instance.ShowActionSheetAsync("Role", "Cancel", "Customer", "Kitchen Staff", "Admin");
        if (string.IsNullOrEmpty(role) || role == "Cancel") return;

        var user = new UserAccount
        {
            FullName = fullName.Trim(),
            Email = email,
            Role = role,
            IsActive = true,
            Password = "Welcome123" // temporary — they can change it from their profile
        };

        if (role == "Customer")
        {
            var companies = await _companyDirectory.GetCompaniesAsync();
            if (companies.Count > 0)
            {
                var companyChoice = await AlertService.Instance.ShowActionSheetAsync(
                    "Company", "Skip", companies.Select(c => c.Name).ToArray());
                var company = companies.FirstOrDefault(c => c.Name == companyChoice);
                if (company is not null)
                {
                    user.CompanyId = company.Id;

                    var locations = await _companyDirectory.GetLocationsAsync(company.Id);
                    if (locations.Count == 1)
                    {
                        user.LocationId = locations[0].Id;
                    }
                    else if (locations.Count > 1)
                    {
                        var locChoice = await AlertService.Instance.ShowActionSheetAsync(
                            "Delivery location", "Skip", locations.Select(l => l.Name).ToArray());
                        user.LocationId = locations.FirstOrDefault(l => l.Name == locChoice)?.Id ?? string.Empty;
                    }

                    var floor = await AlertService.Instance.ShowPromptAsync("Floor / Desk", "Where should deliveries go? (optional)");
                    user.DeliveryFloor = floor?.Trim() ?? string.Empty;
                }
            }
        }

        await _userDirectory.AddUserAsync(user);
        await LoadUsersAsync();

        await AlertService.Instance.ShowAsync("User Added",
            $"{user.FullName} ({user.Role}) can now sign in with {user.Email} and the temporary password \"Welcome123\".", "OK");
    }

    private async Task LoadUsersAsync()
    {
        _allUsers = await _userDirectory.GetUsersAsync();
        ApplyFilters();
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilters();
    partial void OnSelectedRoleFilterChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered.Where(u => u.FullName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                                        || u.Email.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedRoleFilter != "All")
        {
            filtered = filtered.Where(u => u.Role.Equals(SelectedRoleFilter, StringComparison.OrdinalIgnoreCase));
        }

        Users.Clear();
        foreach (var user in filtered)
        {
            Users.Add(user);
        }
    }

    [RelayCommand]
    private async Task ToggleUserStatusAsync(UserAccount user)
    {
        if (user == null) return;

        user.IsActive = !user.IsActive;
        await _userDirectory.UpdateUserAsync(user);
        ApplyFilters();

        string statusText = user.IsActive ? "activated" : "suspended";
        if (Application.Current?.MainPage != null)
        {
            await AlertService.Instance.ShowAsync("User Status Updated", $"{user.FullName}'s account has been {statusText}.", "OK");
        }
    }

    [RelayCommand]
    private async Task ChangeUserRoleAsync(UserAccount user)
    {
        if (user == null || Application.Current?.MainPage == null) return;

        string action = await AlertService.Instance.ShowActionSheetAsync(
            $"Assign New Role for {user.FullName}", "Cancel", "Customer", "Kitchen Staff", "Admin");

        if (!string.IsNullOrEmpty(action) && action != "Cancel")
        {
            user.Role = action;
            await _userDirectory.UpdateUserAsync(user);
            ApplyFilters();
        }
    }
}
