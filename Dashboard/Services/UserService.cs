using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dashboard.Models;

namespace Dashboard.Services
{
    public class UserService : IUserService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public async Task<UsersResponse?> GetUsersAsync()
        {
            try
            {
                var response = await ApiClient.WithAuth().GetAsync("users");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UsersResponse>(json, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task SaveUserAsync(User user)
        {
            if (string.IsNullOrEmpty(AuthSession.Token))
                throw new HttpRequestException("Authentication token missing.");

            var patch = new
            {
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                profilePictureUrl = user.ProfilePictureUrl,
                role = user.Role,
                emailVerified = user.EmailVerified
            };

            var content = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json");

            try
            {
                var response = await ApiClient.WithAuth().PatchAsync($"users/{user.Id}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to save user ({(int)response.StatusCode}): {error}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException($"Network error while saving: {ex.Message}", ex);
            }
        }
    }
}