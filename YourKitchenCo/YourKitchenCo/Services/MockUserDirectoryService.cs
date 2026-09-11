using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

/// <summary>
/// In-memory stand-in for the users table (would map to Supabase auth.users +
/// a profile table). Seeded so LoginPage and AdminUsersPage have something
/// real, and shared as a singleton so admin edits are visible everywhere.
/// </summary>
public class MockUserDirectoryService : IUserDirectoryService
{
    private readonly List<UserAccount> _users = new()
    {
        new UserAccount { FullName = "Alex Mercer", Email = "admin@yourkitchen.co", Role = "Admin", IsActive = true, JoinedDate = DateTime.Now.AddYears(-2) },
        new UserAccount { FullName = "Chef Gordon", Email = "gordon@admin.yourkitchen.co", Role = "Kitchen Staff", IsActive = true, JoinedDate = DateTime.Now.AddMonths(-11) },

        new UserAccount { Id = "seed-john", FullName = "John Doe", Email = "john.doe@ecogra.org", Role = "Customer", IsActive = true, JoinedDate = DateTime.Now.AddMonths(-5), CompanyId = "company-ecogra", LocationId = "loc-ecogra-rosebank" },
        new UserAccount { Id = "seed-sarah", FullName = "Sarah Smith", Email = "sarah.smith@ecogra.org", Role = "Customer", IsActive = true, JoinedDate = DateTime.Now.AddMonths(-2), CompanyId = "company-ecogra", LocationId = "loc-ecogra-rosebank" },
        new UserAccount { Id = "seed-priya", FullName = "Priya Naidoo", Email = "priya.naidoo@tata.co.za", Role = "Customer", IsActive = true, JoinedDate = DateTime.Now.AddMonths(-3), CompanyId = "company-tata", LocationId = "loc-tata-illovo" },
        new UserAccount { Id = "seed-thabo", FullName = "Thabo Nkosi", Email = "thabo.nkosi@rcl.co.za", Role = "Customer", IsActive = true, JoinedDate = DateTime.Now.AddMonths(-1), CompanyId = "company-rcl", LocationId = "loc-rcl-bedfordview" },
    };

    public Task<List<UserAccount>> GetUsersAsync() => Task.FromResult(_users.ToList());

    public Task<UserAccount?> GetByEmailAsync(string email) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<UserAccount> AddUserAsync(UserAccount user)
    {
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task UpdateUserAsync(UserAccount user)
    {
        var existing = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existing is null) return Task.CompletedTask;

        existing.FullName = user.FullName;
        existing.Role = user.Role;
        existing.IsActive = user.IsActive;
        existing.CompanyId = user.CompanyId;
        existing.LocationId = user.LocationId;
        return Task.CompletedTask;
    }
}
