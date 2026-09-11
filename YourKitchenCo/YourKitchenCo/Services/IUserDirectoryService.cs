using System.Collections.Generic;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

public interface IUserDirectoryService
{
    Task<List<UserAccount>> GetUsersAsync();
    Task<UserAccount?> GetByEmailAsync(string email);
    Task<UserAccount> AddUserAsync(UserAccount user);
    Task UpdateUserAsync(UserAccount user);
}
