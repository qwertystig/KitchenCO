using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

/// <summary>
/// In-memory stand-in for the company/company_locations tables. Registered as a
/// singleton so edits persist for the app session. Swap for a Supabase-backed
/// implementation later — nothing that consumes ICompanyDirectoryService needs
/// to change.
/// </summary>
public class MockCompanyDirectoryService : ICompanyDirectoryService
{
    private readonly List<Company> _companies;
    private readonly List<CompanyLocation> _locations;

    public MockCompanyDirectoryService()
    {
        var ecogra = new Company { Id = "company-ecogra", Name = "Ecogra", BillingEmail = "accounts@ecogra.org", MealSubsidyAmount = 80.00m, WhitelistedDomains = new List<string> { "ecogra.org" } };
        var tata = new Company { Id = "company-tata", Name = "TATA", BillingEmail = "accounts@tata.co.za", MealSubsidyAmount = 85.00m, WhitelistedDomains = new List<string> { "tata.co.za" } };
        var rcl = new Company { Id = "company-rcl", Name = "RCL", BillingEmail = "accounts@rcl.co.za", MealSubsidyAmount = 40.00m, WhitelistedDomains = new List<string> { "rcl.co.za" } };

        _companies = new List<Company> { ecogra, tata, rcl };

        _locations = new List<CompanyLocation>
        {
            new() { Id = "loc-ecogra-rosebank", CompanyId = ecogra.Id, Name = "Ecogra - Rosebank", Address = "160 Jan Smuts Ave, Rosebank, Johannesburg", DistanceKm = 8m },
            new() { Id = "loc-tata-illovo", CompanyId = tata.Id, Name = "TATA - Illovo", Address = "39 Ferguson Road, Illovo", DistanceKm = 11m },
            new() { Id = "loc-rcl-bedfordview", CompanyId = rcl.Id, Name = "RCL - Bedfordview", Address = "15 Railey Road, Bedfordview", DistanceKm = 18m },
        };
    }

    public Task<List<Company>> GetCompaniesAsync() => Task.FromResult(_companies.ToList());

    public Task<Company?> GetCompanyAsync(string companyId) =>
        Task.FromResult(_companies.FirstOrDefault(c => c.Id == companyId));

    public Task<List<CompanyLocation>> GetLocationsAsync(string companyId) =>
        Task.FromResult(_locations.Where(l => l.CompanyId == companyId).ToList());

    public Task<CompanyLocation?> GetLocationAsync(string locationId) =>
        Task.FromResult(_locations.FirstOrDefault(l => l.Id == locationId));

    public Task<Company> AddCompanyAsync(Company company)
    {
        _companies.Add(company);
        return Task.FromResult(company);
    }

    public Task UpdateCompanyAsync(Company company)
    {
        var existing = _companies.FirstOrDefault(c => c.Id == company.Id);
        if (existing is null) return Task.CompletedTask;

        existing.Name = company.Name;
        existing.BillingEmail = company.BillingEmail;
        existing.IsActive = company.IsActive;
        return Task.CompletedTask;
    }

    public Task<CompanyLocation> AddLocationAsync(CompanyLocation location)
    {
        _locations.Add(location);
        return Task.FromResult(location);
    }

    public Task UpdateLocationAsync(CompanyLocation location)
    {
        var existing = _locations.FirstOrDefault(l => l.Id == location.Id);
        if (existing is null) return Task.CompletedTask;

        existing.Name = location.Name;
        existing.Address = location.Address;
        existing.DeliveryInstructions = location.DeliveryInstructions;
        existing.IsActive = location.IsActive;
        return Task.CompletedTask;
    }
}
