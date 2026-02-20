using WebGUICertManager.Data;
using WebGUICertManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CERTADMINLib;
using System.Runtime.InteropServices.Marshalling;

namespace WebGUICertManager.Pages.Request
{
    public class DenyModel : PageModel
    {
        private readonly AppDbContext context;

        public DenyModel(AppDbContext context)
        {
            this.context = context;
        }

        public string currentcaconfig { get; set; }

        public int currentrequestid { get; set; }

        [BindProperty]
        public Entries Entries { get; set; } = default!;

        CERTADMINLib.CCertAdmin certadmin = new CERTADMINLib.CCertAdmin();

        public async Task<IActionResult> OnGetAsync(int? id, string? config, string caconfig, int requestid)
        {
            if (id != null && config != null)
            {
                currentcaconfig = config;
                currentrequestid = (int)id;
            }

            if (!string.IsNullOrEmpty(caconfig))
            {
                try
                {
                    certadmin.DenyRequest(caconfig, requestid);
                }
                catch
                {

                }
            }

            if (id == null || config == null)
            {
                return RedirectToPage("./Index");
            }



            var entries = await context.Entries.FirstOrDefaultAsync(e => (e.RequestId == id && e.CAConfig == config));
            if (entries == null)
            {
                return RedirectToPage("./Index");
            }
            else
            {
                Entries = entries;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {


            if (!ModelState.IsValid)
            {
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}
