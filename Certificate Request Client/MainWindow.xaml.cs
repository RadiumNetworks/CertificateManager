using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace CertificateManager
{
    public sealed partial class MainWindow : Window
    {
        private static readonly HttpClient _httpClient;

        static MainWindow()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var apiHost = config["APIHost:URI"] ?? "https://localhost:5301";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiHost),
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

            
        }

        private async void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            var input = Base64RequestInput.Text?.Trim();
            var request = new { Input = input };
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

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var input = Base64RequestInput.Text?.Trim();
            var request = new { Input = input };
            var response = await _httpClient.PostAsJsonAsync("api/submit", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Response>(_jsonOptions);
            OutputBox.Text = result?.Result ?? "No response received";
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
