using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dashboard.Models;

namespace Dashboard.Services
{
    public class AuthService : IAuthService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public async Task<(bool loginSucceeded, string? errorMessage)> LoginAsync(string email, string password)
        {
            var payload = JsonSerializer.Serialize(new { email, password });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
                var response = await ApiClient.Anonymous().PostAsync("auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<LoginResponse>(body, _jsonOptions);

                    AuthSession.Token = data?.Token;
                    AuthSession.ExpiresAt = data?.ExpiresAt;

                    return (true, null);
                }

                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                        or System.Net.HttpStatusCode.BadRequest)
                {
                    return (false, "Invalid email or password.");
                }

                return (false, $"Unexpected server response: {(int)response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Network error: {ex.Message}");
            }
            catch (JsonException)
            {
                return (false, "Invalid server response.");
            }
        }

        public async Task<(bool isAdmin, string? errorMessage)> CheckAdminAsync()
        {
            try
            {
                var response = await ApiClient.WithAuth().GetAsync("users/me");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "Session expired.");

                if (!response.IsSuccessStatusCode)
                    return (false, "Failed to retrieve user profile.");

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);

                bool isAdmin = string.Equals(user?.Role, "Administrator", StringComparison.OrdinalIgnoreCase);

                return (isAdmin, null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Network error: {ex.Message}");
            }
            catch (JsonException)
            {
                return (false, "Invalid server response.");
            }
        }
    }
}