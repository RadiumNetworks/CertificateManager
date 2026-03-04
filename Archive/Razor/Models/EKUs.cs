using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGUICertManager.Models
{
    [Owned]
    public class EKUs
    {
        [Key]
        public int SANId { get; set; }

        [ForeignKey("RequestId,CAConfig")]
        public Entries Entries { get; set; }

        public string? Name { get; set; } = string.Empty;

    }
}
