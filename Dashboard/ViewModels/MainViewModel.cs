using CommunityToolkit.Mvvm.Input;
using Dashboard.Models;
using Dashboard.Services;
using Dashboard.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Dashboard.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IUserService _userService;
    private readonly HashSet<string> _dirtyUserIds = new();

    private CollectionViewSource? _usersViewSource;

    public ObservableCollection<User> Users { get; } = new();

    public ICollectionView? UsersView => _usersViewSource?.View;
    public Action? OnLogoutRequested { get; set; }

    public List<string> Roles { get; } = new() { "Administrator", "Organizer", "Customer" };
    public List<bool> VerificationOptions { get; } = new() { true, false };

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            FilterUsers(_searchText);
        }
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand LogoutCommand { get; }

    public MainViewModel(IUserService? userService = null)
    {
        _userService = userService ?? new UserService();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);

        _ = LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        var usersResponse = await _userService.GetUsersAsync();
        if (usersResponse?.Items is not { Count: > 0 } items)
            return;

        Users.Clear();
        foreach (var user in items)
            Users.Add(user);

        _usersViewSource = new CollectionViewSource { Source = Users };
        OnPropertyChanged(nameof(UsersView));
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

    public void MarkUserDirty(User user)
    {
        if (user.Id is not null)
            _dirtyUserIds.Add(user.Id);
    }

    private async Task RefreshAsync()
    {
        _dirtyUserIds.Clear();
        await LoadUsersAsync();
    }

    private async Task SaveAsync()
    {
        var toSave = Users.Where(u => u.Id is not null && _dirtyUserIds.Contains(u.Id!)).ToList();

        if (toSave.Count == 0)
        {
            MessageBox.Show("No changes to save.");
            return;
        }

        await Task.WhenAll(toSave.Select(u => _userService.SaveUserAsync(u)));

        _dirtyUserIds.Clear();

        MessageBox.Show("Changes saved.");
    }

    private async Task LogoutAsync()
    {
        try
        {
            await ApiClient.WithAuth().PostAsync("auth/logout", null);
        }
        catch
        {

        }

        AuthSession.Clear();

        OnLogoutRequested?.Invoke();
    }
}