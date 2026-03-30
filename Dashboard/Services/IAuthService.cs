using System.Threading.Tasks;

namespace Dashboard.Services;

public interface IAuthService
{
    Task<(bool loginSucceeded, string? errorMessage)> LoginAsync(string email, string password);
    Task<(bool isAdmin, string? errorMessage)> CheckAdminAsync();
}