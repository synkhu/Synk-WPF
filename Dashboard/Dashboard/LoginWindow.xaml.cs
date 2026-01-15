using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter both email and password.");
                return;
            }

            LoginButton.IsEnabled = false;
            bool loginSucceeded = await TryLogin(email, password);
            LoginButton.IsEnabled = true;

            if (!loginSucceeded)
                return;

            string token = App.Current.Properties["AuthToken"] as string;
            var expiresAt = App.Current.Properties["TokenExpires"] as DateTime?;
            if (expiresAt.HasValue && DateTime.Now >= expiresAt.Value)
            {
                MessageBox.Show("Token expired. Please log in again.");
                this.DialogResult = false;
                return;
            }

            bool isAdmin = await CheckAdminRoleAsync(token);

            if (isAdmin)
            {
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("You do not have administrator rights.");
                this.DialogResult = false;
            }
        }

        private async Task<bool> TryLogin(string email, string password)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.synk.hu/");

            var payload = new { email, password };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<LoginResponse>(responseBody, options);

                    App.Current.Properties["AuthToken"] = data?.token;
                    App.Current.Properties["TokenExpires"] = data?.expiresAt;

                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    MessageBox.Show("Invalid email or password.");
                    return false;
                }
                else
                {
                    MessageBox.Show("Login failed: " + response.StatusCode);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show("Network error: " + ex.Message);
                return false;
            }
            catch (JsonException)
            {
                MessageBox.Show("Error parsing server response.");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message);
                return false;
            }
        }

        private async Task<bool> CheckAdminRoleAsync(string token)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.synk.hu/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.GetAsync("users/me");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show("Session expired. Please log in again.");
                    return false;
                }
                else if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Failed to get user role: " + response.StatusCode);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var user = JsonSerializer.Deserialize<UserResponse>(json, options);

                return user != null && string.Equals(user.role, "Administrator", StringComparison.OrdinalIgnoreCase);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show("Network error: " + ex.Message);
                return false;
            }
            catch (JsonException)
            {
                MessageBox.Show("Error parsing server response.");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message);
                return false;
            }
        }
    }

    public class LoginResponse
    {
        public string? token { get; set; }
        public DateTime? expiresAt { get; set; }
    }

    public class UserResponse
    {
        public string? id { get; set; }
        public string? email { get; set; }
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? profilePictureUrl { get; set; }
        public string? role { get; set; }
        public bool emailVerified { get; set; }
        public DateTime createdAt { get; set; }
    }
}