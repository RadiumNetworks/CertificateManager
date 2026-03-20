using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendToSQL.Models
{
    //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
    public class CAInfo
    {
        public byte[] RawCACertificate { get; set; } = null;
        public byte[] RawCRL { get; set; } = null;
        public string SanitizedCAName { get; set; } = string.Empty;
        public string MachineDNSName { get; set; } = string.Empty;
    }
}
