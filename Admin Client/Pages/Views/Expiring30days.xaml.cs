using CertificateManager.Admin.Data;
using CertificateManager.Admin.Data.Services;
using CertificateManager.Admin.Models.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace CertificateManager.Admin.Pages.Views
{
    public sealed partial class Expiring30days : Page
    {
        private ObservableCollection<ExtendedEntry> _entries = new();

        private readonly DatabaseSvc _certificateService = new DatabaseSvc(new SimpleDbContextFactory());

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalCount = 0;
        private int _totalPages = 1;

        public Expiring30days()
        {
            InitializeComponent();
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadEntriesAsync();
        }

        private async Task LoadEntriesAsync()
        {
            try
            {
                using var db = _certificateService.CreateDbContext();

                var expirationDate = DateTime.Now.AddDays(30);
                var filterht = new System.Collections.Hashtable();
                filterht["RequestDisposition"] = "20";

                var query = _certificateService.GetCertificateEntries(db, null, expirationDate, filterht)
                    .Where(e => e.CertificateExpirationDate >= DateTime.Now)
                    .OrderBy(e => e.CertificateExpirationDate);

                _totalCount = await query.CountAsync();
                _totalPages = Math.Max(1, (int)Math.Ceiling((double)_totalCount / _pageSize));

                if (_currentPage > _totalPages)
                    _currentPage = _totalPages;
                if (_currentPage < 1)
                    _currentPage = 1;

                var entries = await query
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToListAsync();

                _entries = new ObservableCollection<ExtendedEntry>(entries);
                EntryDataGrid.ItemsSource = _entries;

                UpdatePaginationUI();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to load data", ex.Message);
            }
        }

        private void UpdatePaginationUI()
        {
            PageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
            TotalCountText.Text = $"{_totalCount:N0} entries";

            FirstPageButton.IsEnabled = _currentPage > 1;
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            LastPageButton.IsEnabled = _currentPage < _totalPages;
        }

        private async void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            await LoadEntriesAsync();
        }

        private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadEntriesAsync();
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadEntriesAsync();
            }
        }

        private async void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            await LoadEntriesAsync();
        }

        private async void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeCombo?.SelectedItem is string sizeStr && int.TryParse(sizeStr, out int newSize))
            {
                _pageSize = newSize;
                _currentPage = 1;
                await LoadEntriesAsync();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = _certificateService.CreateDbContext();

                var expirationDate = DateTime.Now.AddDays(30);
                var filterht = new System.Collections.Hashtable();
                filterht["RequestDisposition"] = "20";

                var allEntries = await _certificateService.GetCertificateEntries(db, null, expirationDate, filterht)
                    .Where(x => x.CertificateExpirationDate >= DateTime.Now)
                    .OrderBy(x => x.CertificateExpirationDate)
                    .ToListAsync();

                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", new[] { ".csv" });
                savePicker.SuggestedFileName = $"ExpiringCertificates_{DateTime.Now:yyyyMMdd}";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("RequestId,CAConfig,Subject,Template,ExpirationDate,Owner,Notes");
                    foreach (var entry in allEntries)
                    {
                        sb.AppendLine($"\"{entry.RequestId}\",\"{Escape(entry.CAConfig)}\",\"{Escape(entry.RequestCommonName)}\",\"{Escape(entry.CertificateTemplate)}\",\"{entry.CertificateExpirationDate}\",\"{Escape(entry.Owner)}\",\"{Escape(entry.Notes)}\"");
                    }
                    await File.WriteAllTextAsync(file.Path, sb.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Export failed", ex.Message);
            }
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\"", "\"\"");
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = $"The database may be unavailable.\n\n{message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch
            {
            }
        }
    }

    internal class SimpleDbContextFactory : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            return new AppDbContext();
        }
    }
}
