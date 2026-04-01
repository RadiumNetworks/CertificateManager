using Azure.Core;
using CertificateManager.Admin.Data;
using CertificateManager.Admin.Data.Services;
using CertificateManager.Admin.Models;
using CertificateManager.Admin.Models.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CertificateManager.Admin.Pages.Certificate
{
    public class DispositionItem
    {
        public string Name { get; set; } = string.Empty;
        public int? Value { get; set; }
        public override string ToString() => Name;
    }

    public class OptionItem
    {
        public string Name { get; set; }
        public int Value { get; set; }
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
        public DispositionOptions DispositionOption { get; set; } = DispositionOptions.Certificate_Issued;

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
                LoadDispositionOptions();

                //using var db = _certificateService.CreateDbContext();
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

        private void LoadDispositionOptions()
        {
            DispositionCodeInput.Items.Clear();
            DispositionCodeInput.Items.Add(new DispositionItem { Name = "(All)", Value = null });

            foreach (DispositionOptions option in Enum.GetValues(typeof(DispositionOptions)))
            {
                DispositionCodeInput.Items.Add(new DispositionItem
                {
                    Name = option.ToString().Replace('_', ' '),
                    Value = (int)option
                });
            }

            DispositionCodeInput.SelectedIndex = 0;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            _filterht["RequestCommonName"] = this.SubjectInput.Text;
            _filterht["Owner"] = this.OwnerInput.Text;
            _filterht["CertificateTemplate"] = this.TemplateInput.Text;
            _filterht["CAConfig"] = (CAInput.SelectedItem?.ToString() == "(All)") ? "" : CAInput.SelectedItem?.ToString() ?? ""; ;

            var selectedDisposition = DispositionCodeInput.SelectedItem as DispositionItem;
            _filterht["RequestDisposition"] = (selectedDisposition?.Value == null) ? "" : selectedDisposition.Value.ToString();

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
                await ShowErrorDialogAsync("Failed to load entries", ex.Message);
            }
        }
    


        private async void EditActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ExtendedEntry entry)
            {
                var detailsPanel = new StackPanel { Spacing = 12, MinWidth = 400 };

                detailsPanel.Children.Add(new TextBlock
                {
                    Text = $"RequestId: {entry.RequestId}\nCA: {entry.CAConfig}\nOwner: {entry.Owner}\nCN: {entry.RequestCommonName}"
                });

                var ownerBox = new TextBox
                {
                    Header = "Owner",
                    Text = entry.Owner ?? string.Empty,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 80,
                    PlaceholderText = "Enter owner here..."
                };
                detailsPanel.Children.Add(ownerBox);

                var notesBox = new TextBox
                {
                    Header = "Notes",
                    Text = entry.Notes ?? string.Empty,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 80,
                    PlaceholderText = "Enter notes here..."
                };
                detailsPanel.Children.Add(notesBox);

                var dialog = new ContentDialog
                {
                    Title = "Certificate Details",
                    Content = detailsPanel,
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        _certificateService.UpdateCertificate(
                            entry.RequestId,
                            entry.CAConfig,
                            ownerBox.Text,
                            notesBox.Text);

                        entry.Notes = notesBox.Text;
                        entry.Owner = ownerBox.Text;
                    }
                    catch (Exception ex)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = $"Failed to save notes: {ex.Message}",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }
            }
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

        private async void RevokeActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ExtendedEntry entry)
            {
                var panel = new StackPanel { Spacing = 8 };

                panel.Children.Add(new TextBlock
                {
                    Text = "Please select a revocation date:",
                    FontSize = 16
                });

                var datePicker = new DatePicker
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    SelectedDate = DateTimeOffset.Now 
                };
                panel.Children.Add(datePicker);

                var comboBox = new ComboBox
                {
                    Width = 200,
                    Margin = new Thickness(0, 10, 0, 0),
                    DisplayMemberPath = "Name", 
                    SelectedValuePath = "Value" 
                };

                var items = new List<OptionItem>
                {
                    new OptionItem { Name = "Unspecified", Value = 0 },
                    new OptionItem { Name = "Key compromise", Value = 1 },
                    new OptionItem { Name = "CA compromise", Value = 2 },
                    new OptionItem { Name = "Affiliation changed", Value = 3 },
                    new OptionItem { Name = "Superseded", Value = 4 },
                    new OptionItem { Name = "Cessation of Operation", Value = 5 },
                    new OptionItem { Name = "Certificate Hold", Value = 6 }

                };

                comboBox.ItemsSource = items;
                comboBox.SelectedIndex = 0;

                panel.Children.Add(comboBox);

                panel.Children.Add(new TextBlock
                {
                    Text = $"\nRequestId: {entry.RequestId}\nCA: {entry.CAConfig}\nOwner: {entry.Owner}\nSerialNumber: {entry.SerialNumber}\nCN: {entry.RequestCommonName}\nEKU:\n{entry.EKUNamesFormatted}\nSAN:\n{entry.SubjectAlternativeNamesFormatted}",
                    FontSize = 16
                });

                var dialog = new ContentDialog
                {
                    Title = $"Certificate Details",
                    Content = panel,
                    PrimaryButtonText = "Ok",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if(result == ContentDialogResult.Primary)
                {
                    try
                    {
                        var certAdmin = new CERTADMINLib.CCertAdmin();
                        certAdmin.RevokeCertificate(entry.CAConfig, entry.SerialNumber, (int)comboBox.SelectedValue, datePicker.Date.UtcDateTime);
                        await ShowDialogAsync("Certificate revoced", $"Certificate {entry.RequestId} {entry.SerialNumber} has been revoked using the Reason {comboBox.SelectedValue} and the effective revocationdate {datePicker.Date.UtcDateTime.ToString("yyyy/MM/dd")}.");

                    }
                    catch (Exception ex)
                    {
                        await ShowDialogAsync("Certificate revocation failed", $"Certificate {entry.RequestId} revocation failed.");
                    }
                }
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
            if (PageSizeCombo?.SelectedItem is string sizeStr && int.TryParse(sizeStr, out int newSize))
            {
                _pageSize = newSize;
                _currentPage = 1;
                await LoadEntriesAsync();
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
