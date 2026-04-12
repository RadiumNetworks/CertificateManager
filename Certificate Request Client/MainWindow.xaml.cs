using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Windows.Storage;

namespace CertificateManager.Request
{
    public sealed partial class MainWindow : Window
    {
        private static readonly HttpClient _httpClient;

        private static string _apihost;

        private static List<string> _certificateAuthorities;

        public ViewModel viewModel { get; } = new();

        static MainWindow()
        {
            string userPath = ApplicationData.Current.LocalFolder.Path;
            string userconfigPath = Path.Combine(userPath, "appsettings.user.json");
            string applicationPath = AppContext.BaseDirectory;
            string appconfigPath = Path.Combine(applicationPath, "appsettings.json");
            string jsonContent = File.ReadAllText(appconfigPath);

            Directory.CreateDirectory(userPath);
            var config = new ConfigurationBuilder()
                .SetBasePath(applicationPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            if (File.Exists(userconfigPath))
            {
                config = new ConfigurationBuilder()
                    .SetBasePath(userPath)
                    .AddJsonFile("appsettings.user.json", optional: false, reloadOnChange: false)
                    .Build();
            }
            else
            {
                File.WriteAllText(userconfigPath, jsonContent);
            }

            _apihost = config["APIHost:URI"] ?? "https://localhost:5301";

            _certificateAuthorities = config.GetSection("CertificateAuthorities")
                .GetChildren().Select(c => c.Value!).ToList();

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_apihost),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MainWindow()
        {
            InitializeComponent();
            AppWindow.SetIcon("Assets\\Logo.ico");
            viewModel.APIString = $"API located at {_apihost}";

            CAConfigCombo.ItemsSource = _certificateAuthorities;
            if (_certificateAuthorities.Count > 0)
                CAConfigCombo.SelectedIndex = 0;
        }

        private async void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            var input = Base64RequestInput.Text?.Trim();
            var request = new { Input = input };
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/parse", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<Response>(_jsonOptions);
                if (result != null)
                {
                    var data = result.Result.ToString().Split(";");
                    if (data.Length > 0)
                    {
                        if (data[0] == "Parsed")
                        {
                            ParsedDataOutput.Text = data[1];
                            ChallengeDataOutput.Text = data[2];
                        }
                        else
                        {
                            ParsedDataOutput.Text = data[3];
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ChallengeDataOutput.Text = ex.Message;
            }
            

        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var input = Base64RequestInput.Text?.Trim();
            var request = new { Input = input };
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/submit", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<Response>(_jsonOptions);
                OutputBox.Text = result?.Result ?? "No response received";
            }
            catch (Exception ex)
            {
                OutputBox.Text = ex.Message;
            }

        }

        private void RetrieveButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class Response
    {
        public string Result { get; set; } = string.Empty;
    }
}
