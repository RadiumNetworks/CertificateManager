
namespace CertificateManager.Models.Views
{
    public class ExtendedEntry
    {
        public int RequestId { get; set; }
        public string CAConfig { get; set; } = string.Empty;
        public string? SerialNumber { get; set; } = string.Empty;
        public string? RequestCommonName { get; set; } = string.Empty;
        public string? RequestCountryRegion { get; set; } = string.Empty;
        public string? RequestCity { get; set; } = string.Empty;
        public string? RequestOrganization { get; set; } = string.Empty;
        public string? RequestOrganizationUnit { get; set; } = string.Empty;
        public string? RequestEMailAddress { get; set; } = string.Empty;
        public string? Owner { get; set; } = string.Empty;
        public string? Notes { get; set; } = string.Empty;
        public string? SubjectAlternativeNames { get; set; } = string.Empty;
        public string? EKUNames { get; set; } = string.Empty;
    }
}
