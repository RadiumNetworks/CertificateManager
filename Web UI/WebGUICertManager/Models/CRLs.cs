using Azure.Core;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebGUICertManager.Models
{
    [PrimaryKey(nameof(CRLRowId), nameof(CAConfig))]
    public class CRLs
    {
        [Required]
        public int CRLRowId { get; set; }

        [Required]
        public string CAConfig { get; set; } = string.Empty;

        public string? CRLNumber { get; set; } = string.Empty;

        public string? CRLRawCRL { get; set; } = string.Empty;
        
        public DateTime? CRLThisUpdate { get; set; }
        
        public DateTime? CRLNextUpdate { get; set; }

        public DateTime? CRLNextPublish { get; set; }

        public ICollection<RevokedCerts> RevokedCerts { get; set; }
    }
}
