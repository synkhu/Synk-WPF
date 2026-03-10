using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Dashboard;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Distinguishes an explicit logout (→ show login again) from the user closing
    // the window directly (→ revoke token server-side, then shut down).
    private bool _isLoggingOut;

    private CollectionViewSource? _usersViewSource;
    private readonly HashSet<string> _dirtyUserIds = [];

    public List<string> Roles { get; } = ["Administrator", "Organizer", "Customer"];

    // Drives the EmailVerified ComboBox; bound via SelectedItem directly to the bool property.
    public List<bool> VerificationOptions { get; } = [true, false];

    public MainWindow()
    {
        InitializeComponent();
        LogoutButton.Click += LogoutButton_Click;
        Closed += MainWindow_Closed;
        SearchBox.TextChanged += SearchBox_TextChanged;
        SaveButton.Click += SaveButton_Click;
        RefreshButton.Click += RefreshButton_Click;
        dataGrid.CellEditEnding += DataGrid_CellEditEnding;
        _ = LoadUsersAsync();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        DragMove();

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(AuthSession.Token))
            await LogoutFromServerAsync();

        AuthSession.Clear();
        _isLoggingOut = true;
        Close();
    }

    private async void MainWindow_Closed(object sender, EventArgs e)
    {
        // If the window was closed without the logout button (e.g. Alt+F4 or the title bar X),
        // we still need to revoke the token on the server before exiting.
        if (!_isLoggingOut && !string.IsNullOrEmpty(AuthSession.Token))
            await LogoutFromServerAsync();

        AuthSession.Clear();

        if (_isLoggingOut)
            ((App)Application.Current).StartLoginFlow();
        else
            Application.Current.Shutdown();
    }

    private async Task LogoutFromServerAsync()
    {
        try
        {
            // The response body is irrelevant; we only care that the server invalidates the token.
            await ApiClient.WithAuth().PostAsync("auth/logout", content: null);
        }
        catch (HttpRequestException ex)
        {
            // A failed server-side logout does not block the local session from being cleared.
            MessageBox.Show(
                $"Server logout failed: {ex.Message}\nYour local session has been cleared.",
                "Logout Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async Task LoadUsersAsync()
    {
        if (string.IsNullOrEmpty(AuthSession.Token))
        {
            MessageBox.Show("Authentication token missing. Please log in again.", "Session Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var usersResponse = await FetchUsersAsync();

        if (usersResponse?.Items is not { Count: > 0 } items)
            return;

        _usersViewSource = new CollectionViewSource { Source = items };
        dataGrid.ItemsSource = _usersViewSource.View;
    }

    private async Task<UsersResponse?> FetchUsersAsync()
    {
        try
        {
            var response = await ApiClient.WithAuth().GetAsync("users");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UsersResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"Failed to load users: {ex.Message}", "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
        catch (JsonException)
        {
            MessageBox.Show("The server returned an unexpected response format.", "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private void FilterUsers(string searchText)
    {
        if (_usersViewSource?.View is null)
            return;

        _usersViewSource.View.Filter = item =>
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            if (item is not User user)
                return false;

            // Each space-separated term must match at least one field — supports "John Smith" queries.
            var terms = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return terms.All(term =>
                (user.FirstName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (user.LastName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (user.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (user.Role?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) =>
        FilterUsers(SearchBox.Text);

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _dirtyUserIds.Clear();
        await LoadUsersAsync();
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit &&
            e.Row.Item is User user &&
            user.Id is not null)
            _dirtyUserIds.Add(user.Id);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var allUsers = (_usersViewSource?.Source as List<User>) ?? [];
        var toSave = allUsers.Where(u => u.Id is not null && _dirtyUserIds.Contains(u.Id!)).ToList();

        if (toSave.Count == 0)
        {
            MessageBox.Show("No changes to save.", "Nothing to Save", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var results = await Task.WhenAll(toSave.Select(SaveUserAsync));
        int saved = results.Count(r => r);

        for (int i = 0; i < toSave.Count; i++)
            if (results[i]) _dirtyUserIds.Remove(toSave[i].Id!);

        MessageBox.Show($"{saved} of {toSave.Count} user(s) saved successfully.", "Save Complete", MessageBoxButton.OK,
            saved == toSave.Count ? MessageBoxImage.Information : MessageBoxImage.Warning);

        if (saved > 0)
            _usersViewSource?.View.Refresh();
    }

    private async Task<bool> SaveUserAsync(User user)
    {
        if (string.IsNullOrEmpty(AuthSession.Token))
        {
            MessageBox.Show("Authentication token missing. Please log in again.", "Session Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        // Only send the fields the API accepts for a PATCH; omit read-only fields like Id and CreatedAt.
        var patch = new
        {
            firstName = user.FirstName,
            lastName = user.LastName,
            email = user.Email,
            profilePictureUrl = user.ProfilePictureUrl,
            role = user.Role,
            emailVerified = user.EmailVerified,
        };

        var content = new StringContent(
            JsonSerializer.Serialize(patch),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await ApiClient.WithAuth().PatchAsync($"users/{user.Id}", content);

            if (response.IsSuccessStatusCode)
                return true;

            var error = await response.Content.ReadAsStringAsync();
            MessageBox.Show($"Failed to save user ({(int)response.StatusCode}): {error}", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"Network error while saving: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}

/// <summary>
/// Maps <see cref="bool"/> EmailVerified values to their display strings and back.
/// Used by the Verification ComboBox so items show "Verified" / "Unverified" instead of True / False.
/// </summary>
[ValueConversion(typeof(bool), typeof(string))]
internal sealed class BoolToVerifiedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "Verified" : "Unverified";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is "Verified";
}

/// <summary>
/// Represents a single user record returned by the Synk API.
/// Properties are mutable so the DataGrid can write back edited values in-place.
/// </summary>
public class User
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("profilePictureUrl")]
    public string? ProfilePictureUrl { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

internal sealed class UsersResponse
{
    [JsonPropertyName("items")]
    public List<User>? Items { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; init; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; init; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; init; }
}
