using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Newtonsoft.Json;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsLoggingOut = false;
        private CollectionViewSource _usersViewSource;
        public List<string> Roles { get; } = new List<string> { "Administrator", "Organizer", "Customer" };

        public MainWindow()
        {
            InitializeComponent();
            LogoutButton.Click += LogoutButton_Click;
            Closed += MainWindow_Closed;
            SearchBox.TextChanged += SearchBox_TextChanged;
            SaveButton.Click += SaveButton_Click;
            _ = LoadUsersAsync();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
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
            Close();
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
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                await client.PostAsync("auth/logout", null);
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
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

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
                    _usersViewSource = new CollectionViewSource();

                    _usersViewSource.Source = usersResponse.Items;

                    dataGrid.ItemsSource = _usersViewSource.View;
                }
            }
            else
            {
                MessageBox.Show("No authentication token available. Please log in.");
            }
        }

        private void FilterUsers(string searchText)
        {
            if (_usersViewSource?.View != null)
            {
                _usersViewSource.View.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(searchText))
                        return true;

                    var user = item as User;

                    if (user == null)
                        return false;

                    var searchTerms = searchText
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(term => term.Trim())
                        .Where(term => !string.IsNullOrEmpty(term))
                        .ToArray();

                    if (searchTerms.Length == 0)
                        return true;

                    return searchTerms.All(term =>
                        user.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        user.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        user.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        user.Role.Contains(term, StringComparison.OrdinalIgnoreCase));
                };
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterUsers(SearchBox.Text);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is User selectedUser)
            {
                bool success = await SaveUserAsync(selectedUser);

                if (success)
                {
                    _usersViewSource?.View.Refresh();
                }
            }
            else
            {
                MessageBox.Show("Please select a user to save.");
            }
        }

        private async Task<bool> SaveUserAsync(User user)
        {
            string token = App.Current.Properties["AuthToken"] as string;

            if (string.IsNullOrEmpty(token) || user == null)
            {
                MessageBox.Show("No token or user selected.");
                return false;
            }

            using var client = new HttpClient();

            client.BaseAddress = new Uri("https://api.synk.hu/");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var userPatch = new
            {
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                profilePictureUrl = user.ProfilePictureUrl,
                role = user.Role
            };

            var json = JsonConvert.SerializeObject(userPatch);

            var content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");

            try
            {
                var response = await client.PatchAsync($"users/{user.Id}", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("User saved successfully!");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();

                    MessageBox.Show("Failed to save user: " + error);

                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving user: " + ex.Message);

                return false;
            }
        }
    }

    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PatchAsync(
            this HttpClient client,
            string requestUri,
            HttpContent content)
        {
            var request = new HttpRequestMessage(
                new HttpMethod("PATCH"),
                requestUri)
            { Content = content };

            return client.SendAsync(request);
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