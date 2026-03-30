using System.Windows;
using System.Windows.Input;

namespace Dashboard.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        if (DataContext is ViewModels.LoginViewModel vm)
        {
            vm.ShowErrorMessage += message =>
                MessageBox.Show(message, "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);

            vm.OnLoginSucceeded += () =>
            {
                DialogResult = true;
            };
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        DragMove();

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }
}