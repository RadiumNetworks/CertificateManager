using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CertificateManager.Admin.Data.Models
{
    public class SQLLog
    {
        public int Id { get; set; }

        public DateTime? LogDate { get; set; }

        public string CAConfig { get; set; } = string.Empty;

        public string SQLStatement { get; set; } = string.Empty;
    }
}
