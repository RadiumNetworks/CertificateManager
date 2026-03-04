using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CertificateManager.Models
{
    [Owned]
    public class EKU
    {
        [Key]
        public int SANId { get; set; }

        [ForeignKey("RequestId,CAConfig")]
        public Entry Entry { get; set; }

        public string? Name { get; set; } = string.Empty;

    }
}
