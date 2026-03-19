using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Certificate_Manager.Data.Models
{
    public class SQLLog
    {
        public int Id { get; set; }

        public DateTime? LogDate { get; set; }

        public string CAConfig { get; set; } = string.Empty;

        public string SQLStatement { get; set; } = string.Empty;
    }
}
