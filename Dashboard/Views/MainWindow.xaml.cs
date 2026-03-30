using Dashboard.Models;
using Dashboard.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dashboard.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Row.Item is User user)
        {
            _vm.MarkUserDirty(user);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        AuthSession.Clear();
        Close();
    }
}