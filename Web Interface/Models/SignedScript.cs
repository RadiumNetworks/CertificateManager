using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CertificateManager.Data.Models
{
    public class SignedScript
    {
        [Required]
        public int Id { get; set; }

        public string? Base64Certificate { get; set; } = string.Empty;

        public string? FileName { get; set; } = string.Empty;

        public string? ScriptContent { get; set; } = string.Empty;

        public string? FileHash { get; set; } = string.Empty;

        public string? SerialNumber { get; set; } = string.Empty;

        public string? Signer { get; set; } = string.Empty;

        public DateTime? SignDate { get; set; } 

        public string? Notes { get; set; } = string.Empty;
    }
}
