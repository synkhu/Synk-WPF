using System.Text;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsLoggingOut = false;

        public MainWindow()
        {
            InitializeComponent();
            LogoutButton.Click += LogoutButton_Click;
            Closed += MainWindow_Closed;
            _ = LoadUsersAsync();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            string token = App.Current.Properties["AuthToken"] as string;

            if (!string.IsNullOrEmpty(token))
            {
                await LogoutFromServerAsync(token);
            }

            App.Current.Properties["AuthToken"] = null;
            App.Current.Properties["TokenExpires"] = null;

            IsLoggingOut = true;
            this.Close();
        }

        private async void MainWindow_Closed(object sender, EventArgs e)
        {
            string token = App.Current.Properties["AuthToken"] as string;
            if (!string.IsNullOrEmpty(token))
            {
                await LogoutFromServerAsync(token);
            }

            App.Current.Properties["AuthToken"] = null;
            App.Current.Properties["TokenExpires"] = null;

            if (IsLoggingOut)
            {
                var app = (App)Application.Current;
                app.StartLoginFlow();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private async Task LogoutFromServerAsync(string token)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.synk.hu/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.PostAsync("auth/logout", null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Logout failed: " + ex.Message);
            }
        }

        private async Task<UsersResponse> GetUsersAsync(string token)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.synk.hu/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.GetAsync("users");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var usersResponse = JsonConvert.DeserializeObject<UsersResponse>(json);
                return usersResponse;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to get users: " + ex.Message);
                return null;
            }
        }

        private async Task LoadUsersAsync()
        {
            string token = App.Current.Properties["AuthToken"] as string;
            if (!string.IsNullOrEmpty(token))
            {
                var usersResponse = await GetUsersAsync(token);
                if (usersResponse != null && usersResponse.Items != null)
                {
                    dataGrid.ItemsSource = usersResponse.Items;
                }
            }
            else
            {
                MessageBox.Show("No authentication token available. Please log in.");
            }
        }
    }

    public class User
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Role { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UsersResponse
    {
        public List<User> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}