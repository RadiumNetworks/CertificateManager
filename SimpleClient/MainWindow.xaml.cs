using Microsoft.UI.Xaml;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleClient
{
    public sealed partial class MainWindow : Window
    {
        // HttpClient pointed at the SimpleAPI
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://localhost:5301"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var input = InputBox.Text?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                OutputBox.Text = "Please enter something.";
                return;
            }

            // Show loading, disable button
            LoadingRing.IsActive = true;
            SubmitButton.IsEnabled = false;
            OutputBox.Text = string.Empty;

            try
            {
                // POST to the API
                var request = new { Input = input };
                var response = await _httpClient.PostAsJsonAsync("api/echo", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<EchoResponse>(_jsonOptions);
                OutputBox.Text = result?.Result ?? "No response";
            }
            catch (Exception ex)
            {
                OutputBox.Text = $"Error: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
                SubmitButton.IsEnabled = true;
            }
        }
    }

    public class EchoResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}
