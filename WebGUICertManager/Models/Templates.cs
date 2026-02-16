using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebGUICertManager.Models
{
    public class Templates
    {
        [Key]
        [Required]
        public string GUID { get; set; } = string.Empty;

        public string? CN { get; set; } = string.Empty;

        public string? DistinguishedName { get; set; } = string.Empty;

        public string? flags { get; set; } = string.Empty;

        public string? msPKICertificateApplicationPolicy { get; set; } = string.Empty;
        public string? msPKICertificateNameFlag { get; set; } = string.Empty;
        public string? msPKICertificatePolicyy { get; set; } = string.Empty;
        public string? msPKICertTemplateOID { get; set; } = string.Empty;
        public string? msPKIEnrollmentFlag { get; set; } = string.Empty;
        public string? msPKIMinimalKeySize { get; set; } = string.Empty;
        public string? msPKIPrivateKeyFlag { get; set; } = string.Empty;
        public string? msPKIRAApplicationPolicies { get; set; } = string.Empty;
        public string? msPKIRAPolicies { get; set; } = string.Empty;
        public string? msPKIRASignature { get; set; } = string.Empty;
        public string? msPKISupersedeTemplates { get; set; } = string.Empty;
        public string? msPKITemplateMinorRevision { get; set; } = string.Empty;
        public string? msPKITemplateSchemaVersion { get; set; } = string.Empty;
        public string? pKICriticalExtensions { get; set; } = string.Empty;
        public string? pKIDefaultCSPs { get; set; } = string.Empty;
        public string? pKIDefaultKeySpec { get; set; } = string.Empty;
        public string? pKIExpirationPeriod { get; set; } = string.Empty;
        public string? pKIExtendedKeyUsage { get; set; } = string.Empty;
        public string? pKIKeyUsage { get; set; } = string.Empty;
        public string? pKIMaxIssuingDepth { get; set; } = string.Empty;
        public string? pKIOverlapPeriod { get; set; } = string.Empty;
        public string? ntSecurityDescriptor { get; set; } = string.Empty;

    }
}
