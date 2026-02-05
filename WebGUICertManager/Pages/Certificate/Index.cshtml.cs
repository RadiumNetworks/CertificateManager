using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebGUICertManager.Data;
using WebGUICertManager.Models;
using System;

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
        public string CurrentStringFilter { get; set; }
        public string CurrentIdFilter { get; set; }
        public string CurrentSort { get; set; }


        public PaginatedList<Entries> Entries { get; set; } = default!;
        //public async Task OnGetAsync()
        //{
        //    Entries = await context.Entries.ToListAsync();
        //}

        public async Task OnGetAsync(string sortOrder, string currentStringFilter, string currentIdFilter, string searchString, string searchId, int? pageIndex)
        {
            CurrentSort = sortOrder;
            SubjectSort = String.IsNullOrEmpty(sortOrder) ? "subject_desc" : "subject";
            IdSort = sortOrder == "Id" ? "id_desc" : "Id";

            if (searchString != null || searchId != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentStringFilter;
                searchId = currentIdFilter;
            }

            CurrentStringFilter = searchString;
            CurrentIdFilter = searchId;

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

            switch (sortOrder)
            {
                case "subject_desc":
                    SortEntries = SortEntries.OrderByDescending(Entry => Entry.RequestCommonName);
                    break;
                case "id_desc":
                    SortEntries = SortEntries.OrderByDescending(Entry => Entry.RequestId);
                    break;
                case "subject":
                    SortEntries = SortEntries.OrderBy(Entry => Entry.RequestCommonName);
                    break;
                case "Id":
                    SortEntries = SortEntries.OrderBy(Entry => Entry.RequestId);
                    break;
                default:
                    SortEntries = SortEntries.OrderByDescending(Entry => Entry.RequestId);
                    break;
            }

            var pageSize = Configuration.GetValue("PageSize", 10);
            // Entries = await SortEntries.AsNoTracking().ToListAsync();
            Entries = await PaginatedList<Entries>.CreateAsync(
                SortEntries.AsNoTracking(), pageIndex ?? 1, pageSize);
        }
    }
}
