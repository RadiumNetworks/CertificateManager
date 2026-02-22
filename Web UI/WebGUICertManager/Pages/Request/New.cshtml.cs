using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebGUICertManager.Data;

namespace WebGUICertManager.Pages.Request
{
    public class NewModel : PageModel
    {
        private readonly AppDbContext context;

        public string RequestOption { get; set; }
        public string CurrentRequestOption { get; set; }
        public string RequestData { get; set; }
        public string CurrentRequestData { get; set; }

        public List<SelectListItem> RequestOptions { get; set; }

        public NewModel(AppDbContext context)
        {
            this.context = context;
        }

        public async Task OnGetAsync(string requestoption, string currentrequestoption, string requestdata, string currentrequestdata)
        {
            RequestData = requestdata;
            CurrentRequestData = currentrequestdata;
            RequestOption = requestoption;
            CurrentRequestOption = currentrequestoption;
            
            RequestOptions = new List<SelectListItem>();
            RequestOptions.Add(new SelectListItem { Value = "1", Text = "INF" });
            RequestOptions.Add(new SelectListItem { Value = "2", Text = "REQ" });

            switch (RequestOption)
            {
                case "1":
                    {
                        break;
                    }
                case "2":
                    {
                        break;
                    }
            }
        }
    }
}
