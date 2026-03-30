using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Dashboard.Services;
using CommunityToolkit.Mvvm.Input;

namespace Dashboard.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly AuthService _authService = new();
    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _isLoggingIn;

    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set
        {
            _isLoggingIn = value;
            OnPropertyChanged();
            ((AsyncRelayCommand)LoginCommand).NotifyCanExecuteChanged();
        }
    }

    public ICommand LoginCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? OnLoginSucceeded;

    public event Action<string>? ShowErrorMessage;

    public LoginViewModel()
    {
        LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsLoggingIn);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ShowErrorMessage?.Invoke("Please enter both email and password.");
            return;
        }

        IsLoggingIn = true;
        try
        {
            var (loginSucceeded, loginError) = await _authService.LoginAsync(Email, Password);
            if (!loginSucceeded)
            {
                ShowErrorMessage?.Invoke(loginError ?? "Login failed.");
                return;
            }

            if (AuthSession.IsExpired)
            {
                ShowErrorMessage?.Invoke("Session expired. Please try again.");
                return;
            }

            var (isAdmin, adminError) = await _authService.CheckAdminAsync();
            if (!isAdmin)
            {
                ShowErrorMessage?.Invoke(adminError ?? "Access denied. Admin required.");
                return;
            }

            OnLoginSucceeded?.Invoke();
        }
        finally
        {
            IsLoggingIn = false;
        }
    }
}