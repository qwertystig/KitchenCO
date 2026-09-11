using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class AdminCompaniesViewModel : ObservableObject
{
    private readonly ICompanyDirectoryService _companyDirectory;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<CompanyRow> Companies { get; } = new();

    public AdminCompaniesViewModel(ICompanyDirectoryService companyDirectory)
    {
        _companyDirectory = companyDirectory;
        _ = LoadCompaniesAsync();
    }

    [RelayCommand]
    private async Task LoadCompaniesAsync()
    {
        IsBusy = true;
        try
        {
            Companies.Clear();

            foreach (var company in await _companyDirectory.GetCompaniesAsync())
            {
                var row = new CompanyRow { Company = company };
                foreach (var location in await _companyDirectory.GetLocationsAsync(company.Id))
                    row.Locations.Add(location);

                Companies.Add(row);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddCompanyAsync()
    {
        if (Application.Current?.MainPage == null) return;

        string name = await AlertService.Instance.ShowPromptAsync("New Company", "Company name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        string billingEmail = await AlertService.Instance.ShowPromptAsync("Billing Contact", "Billing email:", keyboard: Keyboard.Email);

        var company = new Company
        {
            Name = name,
            BillingEmail = billingEmail ?? string.Empty,
            IsActive = true
        };

        await _companyDirectory.AddCompanyAsync(company);
        Companies.Add(new CompanyRow { Company = company });

        await AlertService.Instance.ShowAsync("Company Added", $"{company.Name} has been added. Add at least one delivery location for it next.", "OK");
    }

    [RelayCommand]
    private async Task EditCompanyAsync(CompanyRow row)
    {
        if (row == null || Application.Current?.MainPage == null) return;

        string updatedName = await AlertService.Instance.ShowPromptAsync("Edit Company", "Company name:", initialValue: row.Company.Name);
        if (string.IsNullOrWhiteSpace(updatedName)) return;

        string updatedEmail = await AlertService.Instance.ShowPromptAsync("Billing Contact", "Billing email:", initialValue: row.Company.BillingEmail, keyboard: Keyboard.Email);

        row.Company.Name = updatedName;
        row.Company.BillingEmail = updatedEmail ?? string.Empty;

        await _companyDirectory.UpdateCompanyAsync(row.Company);
        await LoadCompaniesAsync(); // refresh so the CollectionView picks up the renamed company
    }

    [RelayCommand]
    private async Task ToggleCompanyActiveAsync(CompanyRow row)
    {
        if (row == null) return;

        row.Company.IsActive = !row.Company.IsActive;
        await _companyDirectory.UpdateCompanyAsync(row.Company);

        if (Application.Current?.MainPage != null)
        {
            string state = row.Company.IsActive ? "active" : "suspended";
            await AlertService.Instance.ShowAsync("Company Updated", $"{row.Company.Name} is now {state}.", "OK");
        }
    }

    /// <summary>
    /// Subsidy (per-meal ZAR the company covers) and discount (percentage
    /// or flat ZAR off the order total, a separate lever — a company can
    /// have either, both, or neither) — combined into one flow since
    /// there was previously no admin UI for subsidy at all, only seed data.
    /// </summary>
    [RelayCommand]
    private async Task ManageFinancialsAsync(CompanyRow row)
    {
        if (row == null || Application.Current?.MainPage == null) return;

        string subsidyStr = await AlertService.Instance.ShowPromptAsync(
            "Per-Meal Subsidy",
            "Amount the company covers per meal, in Rand (0 for none):",
            initialValue: row.Company.MealSubsidyAmount.ToString("F2"),
            keyboard: Keyboard.Numeric);

        if (subsidyStr is null) return; // cancelled
        if (decimal.TryParse(subsidyStr, out var subsidy))
            row.Company.MealSubsidyAmount = Math.Max(0, subsidy);

        string discountTypeChoice = await AlertService.Instance.ShowActionSheetAsync(
            "Order Discount Type", "Cancel", "None", "Percentage", "Fixed Rand Amount");

        if (string.IsNullOrEmpty(discountTypeChoice) || discountTypeChoice == "Cancel")
        {
            await _companyDirectory.UpdateCompanyAsync(row.Company);
            await LoadCompaniesAsync();
            return;
        }

        row.Company.DiscountType = discountTypeChoice switch
        {
            "Percentage" => DiscountType.Percentage,
            "Fixed Rand Amount" => DiscountType.FixedZar,
            _ => DiscountType.None
        };

        if (row.Company.DiscountType != DiscountType.None)
        {
            string valuePrompt = row.Company.DiscountType == DiscountType.Percentage
                ? "Discount percentage (0-100):"
                : "Discount amount, in Rand:";

            string discountStr = await AlertService.Instance.ShowPromptAsync(
                "Discount Value", valuePrompt,
                initialValue: row.Company.DiscountValue.ToString("F2"),
                keyboard: Keyboard.Numeric);

            if (decimal.TryParse(discountStr, out var discountValue))
            {
                row.Company.DiscountValue = row.Company.DiscountType == DiscountType.Percentage
                    ? Math.Clamp(discountValue, 0, 100)
                    : Math.Max(0, discountValue);
            }
        }
        else
        {
            row.Company.DiscountValue = 0;
        }

        await _companyDirectory.UpdateCompanyAsync(row.Company);
        await LoadCompaniesAsync();

        var discountLabel = row.Company.DiscountType switch
        {
            DiscountType.Percentage => $"{row.Company.DiscountValue}% off orders",
            DiscountType.FixedZar => $"R{row.Company.DiscountValue:F2} off orders",
            _ => "no discount"
        };
        await AlertService.Instance.ShowAsync("Financials Updated",
            $"{row.Company.Name}: R{row.Company.MealSubsidyAmount:F2}/meal subsidy, {discountLabel}.", "OK");
    }

    [RelayCommand]
    private async Task AddLocationAsync(CompanyRow row)
    {
        if (row == null || Application.Current?.MainPage == null) return;

        string name = await AlertService.Instance.ShowPromptAsync("New Location", $"Location name for {row.Company.Name} (e.g. \"Building 2 — Sandton\"):");
        if (string.IsNullOrWhiteSpace(name)) return;

        string address = await AlertService.Instance.ShowPromptAsync("Address", "Delivery address:");

        string distanceStr = await AlertService.Instance.ShowPromptAsync("Delivery Distance", "Distance from the kitchen, in km (used to calculate the delivery fee):", keyboard: Keyboard.Numeric);
        decimal.TryParse(distanceStr, out var distanceKm);

        var location = new CompanyLocation
        {
            CompanyId = row.Company.Id,
            Name = name,
            Address = address ?? string.Empty,
            DistanceKm = distanceKm,
            IsActive = true
        };

        await _companyDirectory.AddLocationAsync(location);
        row.Locations.Add(location);
    }

    [RelayCommand]
    private async Task EditLocationAsync(CompanyLocation location)
    {
        if (location == null || Application.Current?.MainPage == null) return;

        string updatedName = await AlertService.Instance.ShowPromptAsync("Edit Location", "Location name:", initialValue: location.Name);
        if (string.IsNullOrWhiteSpace(updatedName)) return;

        string updatedAddress = await AlertService.Instance.ShowPromptAsync("Address", "Delivery address:", initialValue: location.Address);

        string distanceStr = await AlertService.Instance.ShowPromptAsync("Delivery Distance", "Distance from the kitchen, in km:", initialValue: location.DistanceKm.ToString("F1"), keyboard: Keyboard.Numeric);
        if (decimal.TryParse(distanceStr, out var distanceKm))
            location.DistanceKm = distanceKm;

        location.Name = updatedName;
        location.Address = updatedAddress ?? string.Empty;

        await _companyDirectory.UpdateLocationAsync(location);

        // Force the CollectionView to re-read this item (CompanyLocation
        // isn't an ObservableObject, so a manual refresh keeps this simple).
        var parentRow = Companies.FirstOrDefault(r => r.Locations.Contains(location));
        if (parentRow != null)
        {
            var index = parentRow.Locations.IndexOf(location);
            if (index >= 0)
            {
                parentRow.Locations.RemoveAt(index);
                parentRow.Locations.Insert(index, location);
            }
        }
    }

    /// <summary>
    /// Manages the whitelisted email domains that auto-match employees to
    /// this company at registration. Simple add/remove loop via prompts —
    /// matching the pattern used elsewhere in this admin screen rather than
    /// building a dedicated multi-select UI for what's usually just 1-2 domains.
    /// </summary>
    [RelayCommand]
    private async Task ManageDomainsAsync(CompanyRow row)
    {
        if (row == null || Application.Current?.MainPage == null) return;

        var currentDomains = string.Join(", ", row.Company.WhitelistedDomains);
        string action = await AlertService.Instance.ShowActionSheetAsync(
            currentDomains.Length > 0 ? $"Current domains: {currentDomains}" : "No domains whitelisted yet",
            "Cancel", "Add a Domain", "Remove a Domain");

        if (string.IsNullOrEmpty(action) || action == "Cancel") return;

        if (action == "Add a Domain")
        {
            string newDomain = await AlertService.Instance.ShowPromptAsync(
                "Add Domain", "Email domain to whitelist (no @), e.g. \"ecogra.org\":");

            if (string.IsNullOrWhiteSpace(newDomain)) return;

            var cleaned = newDomain.Trim().TrimStart('@').ToLowerInvariant();
            if (!row.Company.WhitelistedDomains.Contains(cleaned))
                row.Company.WhitelistedDomains.Add(cleaned);
        }
        else if (row.Company.WhitelistedDomains.Count > 0)
        {
            string toRemove = await AlertService.Instance.ShowActionSheetAsync(
                "Remove which domain?", "Cancel", row.Company.WhitelistedDomains.ToArray());

            if (string.IsNullOrEmpty(toRemove) || toRemove == "Cancel") return;
            row.Company.WhitelistedDomains.Remove(toRemove);
        }

        await _companyDirectory.UpdateCompanyAsync(row.Company);
        await AlertService.Instance.ShowAsync(
            "Domains Updated",
            row.Company.WhitelistedDomains.Count > 0
                ? $"{row.Company.Name} now auto-matches: {string.Join(", ", row.Company.WhitelistedDomains)}"
                : $"{row.Company.Name} has no whitelisted domains — employees will need to select it manually at registration.",
            "OK");
    }
}
