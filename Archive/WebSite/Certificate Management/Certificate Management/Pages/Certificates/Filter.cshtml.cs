using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Certificate_Management.Pages.Certificates
{
    public class FilterModel : PageModel
    {
        public CertificateData filterinfo = new CertificateData();

        public void OnGet()
        {

        }

        public void OnPost() 
        {
            filterinfo.CAConfig = Request.Form["CAConfig"];
            filterinfo.RequestID = Request.Form["RequestID"];
            filterinfo.RequestCommonName = Request.Form["Subject"];
        }

    }
}
