using Dashboard.Models;

namespace Dashboard.Services
{
    public interface IUserService
    {
        Task<UsersResponse?> GetUsersAsync();
        Task SaveUserAsync(User user);
    }
}
