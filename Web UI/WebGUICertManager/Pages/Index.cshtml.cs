using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.DirectoryServices;
using System.Security.Principal;
using System.Threading.RateLimiting;

namespace WebGUICertManager.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public string Claims { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }


        public void OnGet()
        {
            string Name = "";
            string groupclaimtype = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid";
            string denyclaimtype = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid";
            Claims = "The user is member of the following groups:" + Environment.NewLine;
            string SIDTranslated = "false";
            foreach (System.Security.Claims.Claim claim in this.User.Claims)
            {
                if (claim.Type == groupclaimtype || claim.Type == denyclaimtype)
                {
                    try
                    {
                        SecurityIdentifier sid = new SecurityIdentifier(claim.Value);
                        foreach (WellKnownSidType type in Enum.GetValues<WellKnownSidType>())
                        {
                            if(sid.IsWellKnown(type))
                            {
                                Claims += claim.Value + " " + type.ToString() + Environment.NewLine;
                                SIDTranslated = "true";
                            }
                        }
                        if (SIDTranslated == "false")
                        {
                            DirectoryEntry entry = new DirectoryEntry($"LDAP://<SID={claim.Value}>");
                            Name = entry.Properties["Name"].Value.ToString();
                            Claims += claim.Value + " " + Name + Environment.NewLine;
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}
