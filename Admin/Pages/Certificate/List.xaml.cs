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
    public sealed partial class List : Page
    {
        private ObservableCollection<ExtendedEntry> _entries = new();

        private readonly CertificateService _certificateService = new CertificateService(new SimpleDbContextFactory());

        private int currentPage = 1;
        private int pageSize = 10;
        private int totalCount = 0;
        private int totalPages = 1;

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
            using var db = _certificateService.CreateDbContext();
            var entries = await Task.Run(() =>
                _certificateService.GetCertificateEntries(db)
                    .OrderBy(e => e.RequestId)
                    .ToList());

            _entries = new ObservableCollection<ExtendedEntry>(entries);
            EntryDataGrid.ItemsSource = _entries;
        }


        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 1;
            filterht["RequestCommonName"] = this.SubjectInput.Text;
            filterht["Owner"] = this.OwnerInput.Text;
            filterht["CertificateTemplate"] = this.TemplateInput.Text;
            filterht["CAConfig"] = this.CAInput.Text;

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
