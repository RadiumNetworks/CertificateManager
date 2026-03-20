using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Certificate_Manager.Models
{
    public class RevokedCert
    {
        [Key]
        public int RevokedCertId { get; set; }

        [ForeignKey("CRLRowId,CAConfig")]
        public CRL CRL { get; set; }

        public string? SerialNumber { get; set; } = string.Empty;

        public string? Reason { get; set; } = string.Empty;

        public DateTime? RevocationDate { get; set; }

    }
}
