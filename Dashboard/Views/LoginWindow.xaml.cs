using System.Windows;
using System.Windows.Input;
using Dashboard.Models;
using Dashboard.Services;

namespace Dashboard.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _authService = new();

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
            var (loginSucceeded, loginError) = await _authService.LoginAsync(email, password);
            if (!loginSucceeded)
            {
                MessageBox.Show(loginError, "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AuthSession.IsExpired)
            {
                MessageBox.Show("The session received from the server has already expired. Please try again.", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                DialogResult = false;
                return;
            }

            var (isAdmin, adminError) = await _authService.CheckAdminAsync();
            if (!isAdmin)
            {
                MessageBox.Show(adminError ?? "Access denied. An Administrator account is required.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            DialogResult = isAdmin;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}