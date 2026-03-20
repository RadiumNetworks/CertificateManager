using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Certificate_Manager.Models
{
    public class SAN
    {
        [Key]
        public int SANId { get; set; }

        [ForeignKey("RequestId,CAConfig")]
        public Entry Entry { get; set; }

        public string? SubjectAlternativeName { get; set; } = string.Empty;
    }
}
