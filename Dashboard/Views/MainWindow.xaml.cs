using Dashboard.Models;
using Dashboard.Services;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Dashboard.Views;

public partial class MainWindow : Window
{
    private bool _isLoggingOut;
    private CollectionViewSource? _usersViewSource;
    private readonly HashSet<string> _dirtyUserIds = new();
    private readonly UserService _userService = new();

    public List<string> Roles { get; } = new() { "Administrator", "Organizer", "Customer" };
    public List<bool> VerificationOptions { get; } = new() { true, false };

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
            await ApiClient.WithAuth().PostAsync("auth/logout", null);
        }
        catch (HttpRequestException ex)
        {
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

        var usersResponse = await _userService.GetUsersAsync();
        if (usersResponse?.Items is not { Count: > 0 } items)
            return;

        _usersViewSource = new CollectionViewSource { Source = items };
        dataGrid.ItemsSource = _usersViewSource.View;
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
        var allUsers = (_usersViewSource?.Source as List<User>) ?? new List<User>();
        var toSave = allUsers.Where(u => u.Id is not null && _dirtyUserIds.Contains(u.Id!)).ToList();

        if (toSave.Count == 0)
        {
            MessageBox.Show("No changes to save.", "Nothing to Save", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var results = await Task.WhenAll(toSave.Select(u => _userService.SaveUserAsync(u)));
        int saved = results.Count(r => r.Success);

        for (int i = 0; i < toSave.Count; i++)
            if (results[i].Success)
                _dirtyUserIds.Remove(toSave[i].Id!);

        MessageBox.Show(
            $"{saved} of {toSave.Count} user(s) saved successfully.",
            "Save Complete",
            MessageBoxButton.OK,
            saved == toSave.Count ? MessageBoxImage.Information : MessageBoxImage.Warning);

        if (saved > 0)
            _usersViewSource?.View.Refresh();
    }
}