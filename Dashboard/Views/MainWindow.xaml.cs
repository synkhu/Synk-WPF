using Dashboard.Models;
using Dashboard.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dashboard.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (DataContext is MainViewModel vm &&
            e.Row.Item is User user)
        {
            vm.MarkUserDirty(user);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            this.DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        AuthSession.Clear();
        this.Close();
    }
}