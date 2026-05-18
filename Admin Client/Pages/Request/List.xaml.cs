using CertificateManager.Admin.Data;
using CertificateManager.Admin.Data.Services;
using CertificateManager.Admin.Models;
using CertificateManager.Admin.Models.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace CertificateManager.Admin.Pages.Request
{
    public class DispositionItem
    {
        public string Name { get; set; } = string.Empty;
        public int? Value { get; set; }
        public override string ToString() => Name;
    }
    public sealed partial class List : Page
    {
        private ObservableCollection<ExtendedEntry> _entries = new();

        private readonly DatabaseSvc _certificateService = new DatabaseSvc(new SimpleDbContextFactory());

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalCount = 0;
        private int _totalPages = 1;


        public enum DispositionOptions
        {
            Under_Review = 9,
            Certificate_Issued = 20,
            Certificate_Revoked = 21,
            Request_Failed = 30,
            Request_Denied = 31
        }
        public DispositionOptions DispositionOption { get; set; } = DispositionOptions.Under_Review;

        private System.Collections.Hashtable _filterht = new System.Collections.Hashtable();
        
        private double? _requestId = null;
        private DateTime? _expirationDate = null;

        public List()
        {
            InitializeComponent();
            this.Loaded += List_Loaded;
        }

        private async void List_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadCAConfigOptionsAsync();

                using var db = _certificateService.CreateDbContext();
                //var entries = await _certificateService.GetCertificateEntries(db)
                //    .OrderBy(e => e.RequestId)
                //    .ToListAsync();

                //_entries = new ObservableCollection<ExtendedEntry>(entries);
                //EntryDataGrid.ItemsSource = _entries;
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to load data", ex.Message);
            }
        }

        private async Task LoadCAConfigOptionsAsync()
        {
            using var db = _certificateService.CreateDbContext();
            var caConfigs = await db.Entry
                .Select(e => e.CAConfig)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            CAInput.Items.Clear();
            CAInput.Items.Add("(All)");
            foreach (var ca in caConfigs)
                CAInput.Items.Add(ca);

            CAInput.SelectedIndex = 0;
        }


        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            _filterht["RequestCommonName"] = this.SubjectInput.Text;
            _filterht["Owner"] = this.OwnerInput.Text;
            _filterht["CertificateTemplate"] = this.TemplateInput.Text;
            _filterht["CAConfig"] = (CAInput.SelectedItem?.ToString() == "(All)") ? "" : CAInput.SelectedItem?.ToString() ?? ""; 
            _filterht["RequestDisposition"] = (int)DispositionOption;

            _expirationDate = (DateInput.SelectedDate.HasValue)
               ? DateInput.SelectedDate.Value.DateTime
               : null;

            try
            {
                _requestId = (int)RequestIdInput.Value;
            }
            catch
            {

            }
            if (_requestId == 0)
            {
                _requestId = null;
            }


            await LoadEntriesAsync();
        }

        private async Task LoadEntriesAsync()
        {
            try
            {
                using var db = _certificateService.CreateDbContext();

                var query = _certificateService.GetCertificateEntries(db, _requestId, _expirationDate, _filterht)
                    .OrderBy(e => e.RequestId);

                _totalCount = await query.CountAsync();
                _totalPages = Math.Max(1, (int)Math.Ceiling((double)_totalCount / _pageSize));


                if (_currentPage > _totalPages)
                    _currentPage = _totalPages;
                if (_currentPage < 1)
                    _currentPage = 1;

                var entries = await Task.Run(() =>
                        query
                        .Skip((_currentPage - 1) * _pageSize)
                        .Take(_pageSize)
                        .ToListAsync());


                _entries = new ObservableCollection<ExtendedEntry>(entries);
                EntryDataGrid.ItemsSource = _entries;

                UpdatePaginationUI();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to load entries", ex.Message);
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
                // Avoid crashing if the dialog itself cannot be shown
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
            _filterht["RequestDisposition"] = (int)DispositionOption;
            if (PageSizeCombo?.SelectedItem is string sizeStr && int.TryParse(sizeStr, out int newSize))
            {
                _pageSize = newSize;
                _currentPage = 1;
                await LoadEntriesAsync();
            }
        }

        private void ColumnVisibility_Changed(object sender, RoutedEventArgs e)
        {
            ColRequesterInfoColumn.Visibility = ColRequesterInfo.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            ColSANColumn.Visibility = ColSAN.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            ColExpirationColumn.Visibility = ColExpiration.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            ColCAConfigColumn.Visibility = ColCAConfig.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            ColDispositionColumn.Visibility = ColDisposition.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            ColRequesterNameColumn.Visibility = ColRequesterName.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            ColTemplateColumn.Visibility = ColTemplate.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            ColEffectiveDateColumn.Visibility = ColEffectiveDate.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
            ColKeyLengthColumn.Visibility = ColKeyLength.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task ShowDialogAsync(string title, string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = $"{message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch
            {
                // Avoid crashing if the dialog itself cannot be shown
            }
        }

        private async void ApproveActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ExtendedEntry entry)
            {
                var dialog = new ContentDialog
                {
                    Title = $"Certificate Details",
                    Content = $"RequestId: {entry.RequestId}\nCA: {entry.CAConfig}\nOwner: {entry.Owner}\nCN: {entry.RequestCommonName}\nEKU:\n{entry.EKUNamesFormatted}\nSAN:\n{entry.SubjectAlternativeNamesFormatted}",
                    PrimaryButtonText = "Ok",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        var certAdmin = new CERTADMINLib.CCertAdmin();
                        certAdmin.ResubmitRequest(entry.CAConfig, entry.RequestId);
                        await ShowDialogAsync("Certificate Request approved", $"Request {entry.RequestId} has been approved successfully.");
                        
                    }
                    catch (Exception ex)
                    {
                        await ShowDialogAsync("Certificate Request approval failed", $"Request {entry.RequestId} approval failed.{ex.Message}");
                    }
                }
            }
        }

        private async void DenyActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ExtendedEntry entry)
            {
                var dialog = new ContentDialog
                {
                    Title = $"Certificate Details",
                    Content = $"RequestId: {entry.RequestId}\nCA: {entry.CAConfig}\nOwner: {entry.Owner}\nCN: {entry.RequestCommonName}\nEKU:\n{entry.EKUNamesFormatted}\nSAN:\n{entry.SubjectAlternativeNamesFormatted}",
                    PrimaryButtonText = "Ok",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        var certAdmin = new CERTADMINLib.CCertAdmin();
                        certAdmin.DenyRequest(entry.CAConfig, entry.RequestId);
                        await ShowDialogAsync("Certificate Request denied", $"Request {entry.RequestId} has been denied successfully.");

                    }
                    catch (Exception ex)
                    {
                        await ShowDialogAsync("Certificate Request denial failed", $"Request {entry.RequestId} denial failed. {ex.Message}");
                    }
                }
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
