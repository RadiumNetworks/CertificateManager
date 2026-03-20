using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Certificate_Manager.Models
{
    [PrimaryKey(nameof(CRLRowId), nameof(CAConfig))]
    public class CRL
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

        public ICollection<RevokedCert> RevokedCert { get; set; }
    }
}
