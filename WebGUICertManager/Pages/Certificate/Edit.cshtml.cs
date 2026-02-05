using WebGUICertManager.Data;
using WebGUICertManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace WebGUICertManager.Pages.Certificate
{
    
    public class EditModel : PageModel
    {
        private readonly AppDbContext context;

        public EditModel(AppDbContext context)
        {
            this.context = context;
        }

        [BindProperty]
        public Entries Entries { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id, string? config)
        {
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
            if(!ModelState.IsValid)
            {
                return Page();
            }

            context.Entries.Update(Entries);
            await context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

    }
}
