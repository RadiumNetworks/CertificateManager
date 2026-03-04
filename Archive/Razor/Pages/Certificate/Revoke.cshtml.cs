using CERTADMINLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices.Marshalling;
using WebGUICertManager.Data;
using WebGUICertManager.Models;

namespace WebGUICertManager.Pages.Certificate
{
    public class RevokeModel : PageModel
    {
        private readonly AppDbContext context;

        public RevokeModel(AppDbContext context)
        {
            this.context = context;
        }

        public string currentcaconfig { get; set; }

        public string currentserialnumber { get; set; }

        public string CurrentRevocation { get; set; } = "0";
        public string RevocationOption { get; set; } = "0";
        public List<SelectListItem> RevocationOptions { get; set; }

        [BindProperty]
        public Entries Entries { get; set; } = default!;

        CERTADMINLib.CCertAdmin certadmin = new CERTADMINLib.CCertAdmin();

        public async Task<IActionResult> OnGetAsync(int? id, string? config, string caconfig, string? serialnumber, int reason)
        {
            RevocationOptions = new List<SelectListItem>();
            RevocationOptions.Add(new SelectListItem { Value = "0", Text = "Unspecified" });
            RevocationOptions.Add(new SelectListItem { Value = "1", Text = "Key compromise" });
            RevocationOptions.Add(new SelectListItem { Value = "2", Text = "CA compromise" });
            RevocationOptions.Add(new SelectListItem { Value = "3", Text = "Affiliation changed" });
            RevocationOptions.Add(new SelectListItem { Value = "4", Text = "Superseded" });
            RevocationOptions.Add(new SelectListItem { Value = "5", Text = "Cessation of Operation" });
            RevocationOptions.Add(new SelectListItem { Value = "6", Text = "Certificate Hold" });

            if (id != null && config != null && serialnumber != null)
            {
                currentcaconfig = config;
                currentserialnumber = serialnumber;
            }
            
            if (!string.IsNullOrEmpty(caconfig) && !string.IsNullOrEmpty(serialnumber))
            {
                try
                {
                    certadmin.RevokeCertificate(caconfig, serialnumber, reason, DateTime.Now);
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
