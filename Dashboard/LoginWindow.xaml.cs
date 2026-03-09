using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace Dashboard;

public partial class LoginWindow : Window
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        DragMove();

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string email = EmailTextBox.Text.Trim();
        string password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Please enter both email and password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoginButton.IsEnabled = false;

        try
        {
            bool loginSucceeded = await TryLoginAsync(email, password);
            if (!loginSucceeded)
                return;

            // Client-side expiry check as an early-out. The server enforces the real boundary.
            if (AuthSession.IsExpired)
            {
                MessageBox.Show("The session received from the server has already expired. Please try again.", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                DialogResult = false;
                return;
            }

            bool isAdmin = await CheckAdminRoleAsync();

            if (!isAdmin)
                MessageBox.Show("Access denied. An Administrator account is required.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);

            DialogResult = isAdmin;
        }
        finally
        {
            // Re-enable regardless of outcome so the user can retry without reopening the window.
            LoginButton.IsEnabled = true;
        }
    }

    private async Task<bool> TryLoginAsync(string email, string password)
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

                return true;
            }

            // Both 400 and 401 indicate bad credentials from this endpoint.
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                    or System.Net.HttpStatusCode.BadRequest)
            {
                MessageBox.Show("Invalid email or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            MessageBox.Show($"Unexpected server response: {(int)response.StatusCode} {response.ReasonPhrase}", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"Network error: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        catch (JsonException)
        {
            MessageBox.Show("The server returned an unexpected response format.", "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private async Task<bool> CheckAdminRoleAsync()
    {
        try
        {
            var response = await ApiClient.WithAuth().GetAsync("users/me");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                MessageBox.Show("Session expired immediately after login. Please try again.", "Session Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Failed to retrieve user profile: {(int)response.StatusCode} {response.ReasonPhrase}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);

            return string.Equals(user?.Role, "Administrator", StringComparison.OrdinalIgnoreCase);
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"Network error while verifying role: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        catch (JsonException)
        {
            MessageBox.Show("The server returned an unexpected response format.", "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}

internal sealed class LoginResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; init; }
}

internal sealed class UserProfile
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("profilePictureUrl")]
    public string? ProfilePictureUrl { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
}
