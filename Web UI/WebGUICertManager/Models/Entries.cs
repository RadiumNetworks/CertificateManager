using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebGUICertManager.Models
{
    [PrimaryKey(nameof(RequestId), nameof(CAConfig))]
    public class Entries
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        public string CAConfig { get; set; } = string.Empty;

        public string? Base64Certificate { get; set; } = string.Empty;

        public string? Base64Request { get; set; } = string.Empty;

        public string? SerialNumber { get; set; } = string.Empty;

        public string? RequestDisposition { get; set; } = string.Empty;

        public string? RequesterName { get; set; } = string.Empty;

        public string? RequestType { get; set; } = string.Empty;

        public string? RequestAttributes { get; set; } = string.Empty;

        public string? IssuedEmailAddress { get; set; } = string.Empty;

        public string? IssuedCommonName { get; set; } = string.Empty;

        public string? IssuedCountryRegion { get; set; } = string.Empty;

        public string? IssuedOrganization { get; set; } = string.Empty;

        public string? IssuedOrganizationUnit { get; set; } = string.Empty;

        public string? CallerName { get; set; } = string.Empty;

        public string? CertificateHash { get; set; } = string.Empty;

        public string? CertificateTemplate { get; set; } = string.Empty;

        public DateTime? CertificateEffectiveDate { get; set; }

        public DateTime? CertificateExpirationDate { get; set; }

        public string? PublicKeyLength { get; set; } = string.Empty;

        public string? PublicKeyAlgorithm { get; set; } = string.Empty;

        public string? RequestCountryRegion { get; set; } = string.Empty;

        public string? RequestOrganization { get; set; } = string.Empty;

        public string? RequestOrganizationUnit { get; set; } = string.Empty;

        public string? RequestCommonName { get; set; } = string.Empty;

        public string? RequestCity { get; set; } = string.Empty;

        public string? RequestTitle { get; set; } = string.Empty;

        public string? RequestEmailAddress { get; set; } = string.Empty;

        public string? TemplateEnrollmentFlags { get; set; } = string.Empty;

        public string? TemplateGeneralFlags { get; set; } = string.Empty;

        public string? TemplatePrivateKeyFlags { get; set; } = string.Empty;

        public string? PublicKeyAlgorithmParameters { get; set; } = string.Empty;

        public DateTime? RevocationDate { get; set; }

        public string? RevocationReason { get; set; } = string.Empty;

        public string? Owner { get; set; } = string.Empty;

        public string? Notes { get; set; } = string.Empty;

        public ICollection<SANs> SANs { get; set; }

        public ICollection<EKUs> EKUs { get; set; }
    }
}
