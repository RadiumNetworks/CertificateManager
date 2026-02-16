using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using WebGUICertManager.Data;
using WebGUICertManager.Models;

namespace WebGUICertManager.Pages.Request
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext context;
        private readonly IConfiguration Configuration;

        public IndexModel(AppDbContext context, IConfiguration configuration)
        {
            this.context = context;
            Configuration = configuration;
        }

        public string IdSort { get; set; }
        public string SubjectSort { get; set; }
        public string ExpirationDateSort { get; set; }
        public string OwnerSort { get; set; }
        public string CurrentStringFilter { get; set; }
        public string CurrentIdFilter { get; set; }
        public string CurrentDateFilter { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentDisposition {  get; set; } = "9";
        public string DispositionOption { get; set; } = "9";

        public List<SelectListItem> AvailableOptions { get; set; }
        public List<SelectListItem> DispositionOptions { get; set; }

        public string SelectedOption { get; set; }

        public PaginatedList<Entries> Entries { get; set; } = default!;

        public void DownloadExport(List<Entries> ExportEntries, string[]? ExportFilter)
        {
            {
                string csv = string.Empty;
                string date = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");
                using (StreamWriter writer = new StreamWriter(("c:\\temp\\certificatelist_" + date + ".csv"), true))
                {
                    if (ExportFilter != null)
                    {
                        foreach (var ExportEntry in ExportEntries)
                        {
                            string line = "'";
                            foreach (var elem in ExportFilter)
                            {
                                try
                                {
                                    var property = ExportEntry.GetType().GetProperty(elem);
                                    object val = property.GetValue(ExportEntry);
                                    if (val != null)
                                    {
                                        line += ("" + val.ToString().Replace("\n", "").Replace("\r", "") + "','");
                                    }
                                    else
                                    {
                                        line += "','";
                                    }
                                }
                                catch
                                {

                                }

                            }
                            line += "'";
                            writer.WriteLine(line);
                        }
                    }
                    else
                    {
                        var properties = typeof(Entries).GetProperties();
                        foreach (var ExportEntry in ExportEntries)
                        {
                            csv += String.Join(",", properties.Select(p => p.GetValue(ExportEntry)));
                            writer.WriteLine(String.Join(",", properties.Select(p => "'" + p.GetValue(ExportEntry) + "'")));
                        }
                    }
                }
            }
        }

        public async Task OnGetAsync(string sortOrder, string currentStringFilter, string currentIdFilter, string currentDateFilter, string searchString, string searchId, string searchDate, string Export, string ExportFilter, string DispositionOption, int? pageIndex)
        {
            IdSort = sortOrder == "Id" ? "id_desc" : "Id";
            SubjectSort = sortOrder == "subject" ? "subject_desc" : "subject";
            OwnerSort = sortOrder == "owner" ? "owner_desc" : "owner";
            ExpirationDateSort = sortOrder == "expirationdate" ? "expirationdate_desc" : "expirationdate";

            CurrentDisposition = DispositionOption;

            AvailableOptions = new List<SelectListItem>();
            PropertyInfo[] properties = typeof(Entries).GetProperties();
            foreach (var property in properties)
            {
                AvailableOptions.Add(new SelectListItem { Value = property.Name, Text = property.Name });
            }

            DispositionOptions = new List<SelectListItem>();
            DispositionOptions.Add(new SelectListItem { Value = "9", Text = "Request under review" });

            if (searchString != null || searchId != null || searchDate != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentStringFilter;
                searchId = currentIdFilter;
                searchDate = currentDateFilter;
            }

            if (ExportFilter != null)
            {
                searchString = currentStringFilter;
                searchId = currentIdFilter;
                searchDate = currentDateFilter;
                sortOrder = CurrentSort;
            }

            CurrentStringFilter = searchString;
            CurrentIdFilter = searchId;
            CurrentDateFilter = searchDate;
            CurrentSort = sortOrder;




            IQueryable<Entries> SortEntries = from Entry in context.Entries
                                              select Entry;

            if (!String.IsNullOrEmpty(searchString))
            {
                SortEntries = SortEntries.Where(Entry => (Entry.RequestCommonName.ToUpper().Contains(searchString.ToUpper())
                || Entry.Owner.ToUpper().Contains(searchString.ToUpper())
                || Entry.Notes.ToUpper().Contains(searchString.ToUpper())));
            }
            if (!String.IsNullOrEmpty(searchId))
            {
                SortEntries = SortEntries.Where(Entry => (Entry.RequestId.ToString().Contains(searchId)));
            }
            if (!String.IsNullOrEmpty(searchDate))
            {
                DateTime timestamp = DateTime.Parse(searchDate);
                SortEntries = SortEntries.Where(Entry => Entry.CertificateExpirationDate < timestamp);
            }
            if (!String.IsNullOrEmpty(DispositionOption))
            {
                SortEntries = SortEntries.Where(Entry => (Entry.RequestDisposition.ToString().Contains(DispositionOption)));
            }
            else
            {
                SortEntries = SortEntries.Where(Entry => (Entry.RequestDisposition.ToString().Contains(this.DispositionOption)));
            }

            if ((Export != null) && (ExportFilter != null))
            {
                string[] ExportProperties = ExportFilter.Split(",");
                var ExportEntries = new List<Entries>();
                ExportEntries.AddRange(SortEntries);
                DownloadExport(ExportEntries, ExportProperties);
            }
            else if (Export != null)
            {
                var ExportEntries = new List<Entries>();
                ExportEntries.AddRange(SortEntries);
                DownloadExport(ExportEntries, null);
            }


            switch (sortOrder)
            {
                case "subject_desc":
                    SortEntries = SortEntries.OrderByDescending(Entry => Entry.RequestCommonName);
                    break;
                case "subject":
                    SortEntries = SortEntries.OrderBy(Entry => Entry.RequestCommonName);
                    break;
                case "owner_desc":
                    SortEntries = SortEntries.OrderByDescending(Entry => Entry.Owner);
                    break;
                case "owner":
                    SortEntries = SortEntries.OrderBy(Entry => Entry.Owner);
                    break;
                case "expirationdate_desc":
                    SortEntries = SortEntries.OrderByDescending(Entry => Entry.CertificateExpirationDate);
                    break;
                case "expirationdate":
                    SortEntries = SortEntries.OrderBy(Entry => Entry.CertificateExpirationDate);
                    break;
                case "id_desc":
                    SortEntries = SortEntries.OrderByDescending(Entry => Entry.RequestId);
                    break;
                case "Id":
                    SortEntries = SortEntries.OrderBy(Entry => Entry.RequestId);
                    break;
                default:
                    SortEntries = SortEntries.OrderByDescending(Entry => Entry.RequestId);
                    break;
            }

            var pageSize = Configuration.GetValue("PageSize", 10);
            Entries = await PaginatedList<Entries>.CreateAsync(
                SortEntries.AsNoTracking(), pageIndex ?? 1, pageSize);
        }
    }
}
