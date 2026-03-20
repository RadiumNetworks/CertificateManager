using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendToSQL.Models
{
    //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getrequestproperty
    public class CARequest
    {
        public int RequestId { get; set; } = 0;
        public int? RequestType { get; set; } = null;
        public int? StatusCode { get; set; } = null;
        public int? Disposition { get; set; } = null;
        public string DispositionMessage { get; set; } = string.Empty;
        public DateTime ResolvedWhen { get; set; }
        public DateTime SubmittedWhen { get; set; }
        public byte[] RawCertificate { get; set; } = null;
        public string RequesterName { get; set; } = string.Empty;
        public string DistinguishedName { get; set; } = string.Empty;
        public string RequestAttributes { get; set; } = string.Empty;
        public string CommonName { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string OrgUnit { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string Locality { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public byte[] RawRequest { get; set; } = null;
        public string CallerName { get; set; } = string.Empty;
    }
}
