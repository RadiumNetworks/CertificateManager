using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq.Expressions;
using WebGUICertManager.Data;
using WebGUICertManager.Models;

namespace WebGUICertManager.Pages.Certificate
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


        public PaginatedList<Entries> Entries { get; set; } = default!;

        public async Task OnGetAsync(string sortOrder, string currentStringFilter, string currentIdFilter, string currentDateFilter, string searchString, string searchId, string searchDate, int? pageIndex)
        {
            CurrentSort = sortOrder;
            IdSort = sortOrder == "Id" ? "id_desc" : "Id";
            SubjectSort = sortOrder == "subject" ? "subject_desc" : "subject";
            OwnerSort = sortOrder == "owner" ? "owner_desc" : "owner";
            ExpirationDateSort = sortOrder == "expirationdate" ? "expirationdate_desc" : "expirationdate";

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

            CurrentStringFilter = searchString;
            CurrentIdFilter = searchId;
            CurrentDateFilter = searchDate;

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
