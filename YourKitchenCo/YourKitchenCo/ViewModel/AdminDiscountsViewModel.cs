using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class AdminDiscountsViewModel : ObservableObject
{
    private const string AllCompaniesOption = "All Companies";

    private readonly IDiscountService _discountService;
    private readonly ICompanyDirectoryService _companyDirectory;

    [ObservableProperty]
    private string _newCode = string.Empty;

    [ObservableProperty]
    private string _newPercentage = string.Empty;

    [ObservableProperty]
    private string _selectedCompanyOption = AllCompaniesOption;

    [ObservableProperty]
    private string _newExpires = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<string> CompanyOptions { get; } = new() { AllCompaniesOption };

    public ObservableCollection<Discount> Discounts { get; } = new();

    public AdminDiscountsViewModel(IDiscountService discountService, ICompanyDirectoryService companyDirectory)
    {
        _discountService = discountService;
        _companyDirectory = companyDirectory;

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;

        var companies = await _companyDirectory.GetCompaniesAsync();
        CompanyOptions.Clear();
        CompanyOptions.Add(AllCompaniesOption);
        foreach (var company in companies) CompanyOptions.Add(company.Name);

        var discounts = await _discountService.GetDiscountsAsync();
        Discounts.Clear();
        foreach (var discount in discounts) Discounts.Add(discount);

        IsBusy = false;
    }

    [RelayCommand]
    private async Task AddDiscountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCode) ||
            !decimal.TryParse(NewPercentage, NumberStyles.Number, CultureInfo.InvariantCulture, out var percentage) ||
            percentage <= 0)
        {
            await AlertService.Instance.ShowAsync("Missing Information", "Please provide a code and a valid percentage.", "OK");
            return;
        }

        DateTime? expires = null;
        if (!string.IsNullOrWhiteSpace(NewExpires) &&
            DateTime.TryParse(NewExpires, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedExpiry))
        {
            expires = parsedExpiry;
        }

        string? companyId = null;
        string? companyName = null;
        if (SelectedCompanyOption != AllCompaniesOption)
        {
            var companies = await _companyDirectory.GetCompaniesAsync();
            var match = companies.FirstOrDefault(c => c.Name == SelectedCompanyOption);
            companyId = match?.Id;
            companyName = match?.Name;
        }

        var discount = new Discount
        {
            Code = NewCode.Trim().ToUpperInvariant(),
            Percentage = percentage,
            Active = true,
            Expires = expires,
            CompanyId = companyId,
            CompanyName = companyName,
        };

        await _discountService.AddDiscountAsync(discount);
        Discounts.Insert(0, discount);

        NewCode = string.Empty;
        NewPercentage = string.Empty;
        NewExpires = string.Empty;
        SelectedCompanyOption = AllCompaniesOption;
    }

    [RelayCommand]
    private async Task ToggleDiscountActiveAsync(Discount discount)
    {
        if (discount == null) return;

        discount.Active = !discount.Active;
        await _discountService.UpdateDiscountAsync(discount);
        await LoadAsync(); // refresh so the CollectionView picks up the new Active state
    }

    [RelayCommand]
    private async Task DeleteDiscountAsync(Discount discount)
    {
        if (discount == null) return;

        bool confirmed = await AlertService.Instance.ShowConfirmAsync(
            "Delete Discount", $"Remove code {discount.Code}?", "Delete", "Cancel");
        if (!confirmed) return;

        await _discountService.DeleteDiscountAsync(discount.Id);
        Discounts.Remove(discount);
    }
}
