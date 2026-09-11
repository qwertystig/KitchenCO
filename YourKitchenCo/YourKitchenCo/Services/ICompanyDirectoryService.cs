using System.Collections.Generic;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

public interface ICompanyDirectoryService
{
    Task<List<Company>> GetCompaniesAsync();
    Task<Company?> GetCompanyAsync(string companyId);
    Task<List<CompanyLocation>> GetLocationsAsync(string companyId);
    Task<CompanyLocation?> GetLocationAsync(string locationId);

    Task<Company> AddCompanyAsync(Company company);
    Task UpdateCompanyAsync(Company company);

    Task<CompanyLocation> AddLocationAsync(CompanyLocation location);
    Task UpdateLocationAsync(CompanyLocation location);
}
