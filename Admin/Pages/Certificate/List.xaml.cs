using Azure.Core;
using Certificate_Manager.Data;
using Certificate_Manager.Data.Services;
using Certificate_Manager.Models;
using Certificate_Manager.Models.Views;
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

namespace Certificate_Manager.Pages.Certificate
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

        private readonly CertificateService _certificateService = new CertificateService(new SimpleDbContextFactory());

        private int currentPage = 1;
        private int pageSize = 10;
        private int totalCount = 0;
        private int totalPages = 1;


        public enum DispositionOptions
        {
            Under_Review = 9,
            Certificate_Issued = 20,
            Certificate_Revoked = 21,
            Request_Failed = 30,
            Request_Denied = 31
        }
        public DispositionOptions DispositionOption { get; set; } = DispositionOptions.Certificate_Issued;

        private System.Collections.Hashtable filterht = new System.Collections.Hashtable();
        private double? requestId = null;
        private DateTime? expirationDate = null;

        public List()
        {
            InitializeComponent();
            this.Loaded += List_Loaded;
        }

        private async void List_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCAConfigOptionsAsync();
            LoadDispositionOptions();

            using var db = _certificateService.CreateDbContext();
            var entries = await Task.Run(() =>
                _certificateService.GetCertificateEntries(db)
                    .OrderBy(e => e.RequestId)
                    .ToList());

            _entries = new ObservableCollection<ExtendedEntry>(entries);
            EntryDataGrid.ItemsSource = _entries;
        }

        private async Task LoadCAConfigOptionsAsync()
        {
            using var db = _certificateService.CreateDbContext();
            var caConfigs = await Task.Run(() =>
                db.Entry
                    .Select(e => e.CAConfig)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList());

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
            currentPage = 1;
            filterht["RequestCommonName"] = this.SubjectInput.Text;
            filterht["Owner"] = this.OwnerInput.Text;
            filterht["CertificateTemplate"] = this.TemplateInput.Text;
            filterht["CAConfig"] = (CAInput.SelectedItem?.ToString() == "(All)") ? "" : CAInput.SelectedItem?.ToString() ?? ""; ;

            var selectedDisposition = DispositionCodeInput.SelectedItem as DispositionItem;
            filterht["RequestDisposition"] = (selectedDisposition?.Value == null) ? "" : selectedDisposition.Value.ToString();

            expirationDate = (DateInput.SelectedDate.HasValue)
               ? DateInput.SelectedDate.Value.DateTime
               : null;

            try
            {
                requestId = (int)RequestIdInput.Value;
            }
            catch
            {

            }
            if (requestId == 0)
            {
                requestId = null;
            }
            

            await LoadEntriesAsync();
        }

        private async Task LoadEntriesAsync()
        {
            if (_certificateService == null)
            {
                
            }
            using var db = _certificateService.CreateDbContext();

            var query = _certificateService.GetCertificateEntries(db, requestId, expirationDate, filterht)
                .OrderBy(e => e.RequestId);

            totalCount = await Task.Run(() => query.Count());
            totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

            if (currentPage > totalPages)
                currentPage = totalPages;
            if (currentPage < 1)
                currentPage = 1;

            var entries = await Task.Run(() =>
                    query
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList());


            _entries = new ObservableCollection<ExtendedEntry>(entries);
            EntryDataGrid.ItemsSource = _entries;

            UpdatePaginationUI();
        }

        private async void EditActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ExtendedEntry entry)
            {
                // TODO: Replace with your desired action (e.g. navigate to detail page)
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

        private async void RevokeActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ExtendedEntry entry)
            {
                var dialog = new ContentDialog
                {
                    Title = $"Certificate Details",
                    Content = $"RequestId: {entry.RequestId}\nCA: {entry.CAConfig}\nOwner: {entry.Owner}\nCN: {entry.RequestCommonName}",
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void UpdatePaginationUI()
        {
            PageInfoText.Text = $"Page {currentPage} of {totalPages}";
            TotalCountText.Text = $"{totalCount:N0} entries";

            FirstPageButton.IsEnabled = currentPage > 1;
            PreviousPageButton.IsEnabled = currentPage > 1;
            NextPageButton.IsEnabled = currentPage < totalPages;
            LastPageButton.IsEnabled = currentPage < totalPages;
        }

        private async void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 1;
            await LoadEntriesAsync();
        }

        private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                await LoadEntriesAsync();
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                await LoadEntriesAsync();
            }
        }

        private async void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = totalPages;
            await LoadEntriesAsync();
        }

        private async void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeCombo?.SelectedItem is string sizeStr && int.TryParse(sizeStr, out int newSize))
            {
                pageSize = newSize;
                currentPage = 1;
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
