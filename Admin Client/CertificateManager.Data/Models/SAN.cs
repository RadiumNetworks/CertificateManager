using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CertificateManager.Admin.Models
{
    public class SAN
    {
        [Key]
        public int SANId { get; set; }

        [ForeignKey("RequestId,CAConfig")]
        public required Entry Entry { get; set; }

        public string? SubjectAlternativeName { get; set; } = string.Empty;
    }
}
