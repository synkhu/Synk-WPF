using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dashboard.Models;

namespace Dashboard.Services
{
    public class UserService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// Fetch all users from the API.
        /// </summary>
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
                // Optionally log or handle network errors
                return null;
            }
            catch (JsonException)
            {
                // Optionally log or handle parse errors
                return null;
            }
        }

        /// <summary>
        /// Save an individual user via PATCH.
        /// </summary>
        public async Task<(bool Success, string? Error)> SaveUserAsync(User user)
        {
            if (string.IsNullOrEmpty(AuthSession.Token))
                return (false, "Authentication token missing.");

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

                if (response.IsSuccessStatusCode)
                    return (true, null);

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to save user ({(int)response.StatusCode}): {error}");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Network error while saving: {ex.Message}");
            }
        }
    }
}