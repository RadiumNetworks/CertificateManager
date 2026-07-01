using CertificateManager.Admin.Data;
using CertificateManager.Admin.Data.Services;
using CertificateManager.Admin.Models;
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
    public sealed partial class Wildcard : Page
    {
        private ObservableCollection<ExtendedEntry> _entries = new();

        private readonly DatabaseSvc _certificateService = new DatabaseSvc(new SimpleDbContextFactory());

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalCount = 0;
        private int _totalPages = 1;

        public Wildcard()
        {
            InitializeComponent();
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadEntriesAsync();
        }

        private IQueryable<ExtendedEntry> GetWildcardQuery(AppDbContext db)
        {
            return db.Entry
                .Include(e => e.SAN)
                .Include(e => e.EKU)
                .Where(e => e.RequestCommonName != null && e.RequestCommonName.Contains("*")
                    || e.SAN.Any(s => s.SubjectAlternativeName != null && s.SubjectAlternativeName.Contains("*")))
                .Where(e => e.RequestDisposition == "20" && e.CertificateExpirationDate >= DateTime.Now)
                .Select(e => new ExtendedEntry
                {
                    RequestId = e.RequestId,
                    CAConfig = e.CAConfig,
                    SerialNumber = e.SerialNumber,
                    RequestCommonName = e.RequestCommonName,
                    RequestCountryRegion = e.RequestCountryRegion,
                    RequestCity = e.RequestCity,
                    RequestOrganization = e.RequestOrganization,
                    RequestOrganizationUnit = e.RequestOrganizationUnit,
                    RequestEMailAddress = e.RequestEmailAddress,
                    Owner = e.Owner,
                    Notes = e.Notes,
                    CertificateExpirationDate = e.CertificateExpirationDate,
                    CertificateEffectiveDate = e.CertificateEffectiveDate,
                    RequestDisposition = e.RequestDisposition,
                    RequesterName = e.RequesterName,
                    CertificateTemplate = e.CertificateTemplate,
                    PublicKeyLength = e.PublicKeyLength,
                    SubjectAlternativeNames = string.Join(", ", e.SAN.Select(s => s.SubjectAlternativeName)),
                    EKUNames = string.Join(", ", e.EKU.Select(s => s.Name))
                });
        }

        private async Task LoadEntriesAsync()
        {
            try
            {
                using var db = _certificateService.CreateDbContext();

                var query = GetWildcardQuery(db).OrderBy(e => e.RequestId);

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

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = _certificateService.CreateDbContext();

                var allEntries = await GetWildcardQuery(db)
                    .OrderBy(x => x.RequestId)
                    .ToListAsync();

                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", new[] { ".csv" });
                savePicker.SuggestedFileName = $"WildcardCertificates_{DateTime.Now:yyyyMMdd}";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("RequestId,CAConfig,Subject,SAN,Template,ExpirationDate,Owner,Notes");
                    foreach (var entry in allEntries)
                    {
                        sb.AppendLine($"\"{entry.RequestId}\",\"{Escape(entry.CAConfig)}\",\"{Escape(entry.RequestCommonName)}\",\"{Escape(entry.SubjectAlternativeNames)}\",\"{Escape(entry.CertificateTemplate)}\",\"{entry.CertificateExpirationDate}\",\"{Escape(entry.Owner)}\",\"{Escape(entry.Notes)}\"");
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
}
