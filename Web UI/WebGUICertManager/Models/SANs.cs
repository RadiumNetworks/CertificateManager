using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGUICertManager.Models
{
    public class SANs
    {
        [Key]
        public int SANId { get; set; }

        [ForeignKey("RequestId,CAConfig")]
        public Entries Entries { get; set; }

        public string? SubjectAlternativeName { get; set; } = string.Empty;
    }
}
