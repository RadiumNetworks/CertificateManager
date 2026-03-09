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

namespace Certificate_Manager.Pages.Certificate
{
    public sealed partial class List : Page
    {
        private ObservableCollection<ExtendedEntry> _entries = new();

        private readonly CertificateService _certificateService;

        public List()
        {
            InitializeComponent();
            _certificateService = new CertificateService(new SimpleDbContextFactory());
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
            int? requestId = null;
            DateTime? expirationDate = null;

            System.Collections.Hashtable filterht = new System.Collections.Hashtable();

            string CAInput = this.CAInput.Text;
            string OwnerInput = this.OwnerInput.Text;
            string SubjectInput = this.SubjectInput.Text;
            string TemplateInput = this.TemplateInput.Text;
            double RequestIDInput = this.RequestIdInput.Value;

            filterht["RequestCommonName"] = SubjectInput;
            filterht["Owner"] = OwnerInput;
            filterht["CertificateTemplate"] = TemplateInput;
            filterht["CAConfig"] = CAInput;

            using var db = _certificateService.CreateDbContext();
            var entries = await Task.Run(() =>
                _certificateService.GetCertificateEntries(db,requestId,expirationDate,filterht)
                    .OrderBy(e => e.RequestId)
                    .ToList());

            _entries = new ObservableCollection<ExtendedEntry>(entries);
            EntryDataGrid.ItemsSource = _entries;
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
