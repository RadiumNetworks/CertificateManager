using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGUICertManager.Models
{
    public class RevokedCerts
    {
        [Key]
        public int RevokedCertId { get; set; }

        [ForeignKey("CRLRowId,CAConfig")]
        public CRLs CRLs { get; set; }

        public string? SerialNumber { get; set; } = string.Empty;

        public string? Reason { get; set; } = string.Empty;

        public DateTime? RevocationDate { get; set; }

    }
}
