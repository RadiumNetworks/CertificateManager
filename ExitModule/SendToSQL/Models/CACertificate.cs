using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendToSQL.Models
{
    //https://learn.microsoft.com/en-us/windows/win32/api/certif/nf-certif-icertserverexit-getcertificateproperty
    public class CACertificate
    {
        public int RequestId { get; set; } = 0;
        public int? PublicKeyLength { get; set; } = null;
        public string PublicKeyAlgorithm { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string CertificateTemplate { get; set; } = string.Empty;
        public string CertificateHash { get; set; } = string.Empty;
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        public byte[] RawCertificate { get; set; } = null;
        public byte[] RawPublicKey { get; set; } = null;
        public byte[] RawPublicKeyAlgorithmParameters { get; set; } = null;
        public int? RequestType { get; set; } = null;
        public string CommonName { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string OrgUnit { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Locality { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string CallerName { get; set; } = string.Empty;
        public string TemplateEnrollmentFlags { get; set; } = string.Empty;
        public string TemplateGeneralFlags { get; set; } = string.Empty;
        public string TemplatePrivateKeyFlags { get; set; } = string.Empty;
        public string PublicKeyAlgorithmParameters { get; set; } = string.Empty;
        public DateTime RevocationDate { get; set; }
        public string RevocationReason { get; set; } = string.Empty;
    }
}
