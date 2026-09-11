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

    public AdminUsersViewModel(IUserDirectoryService userDirectory)
    {
        _userDirectory = userDirectory;
        _ = LoadUsersAsync();
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
