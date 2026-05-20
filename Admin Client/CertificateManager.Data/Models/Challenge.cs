using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CertificateManager.Admin.Models
{
    public class Challenge
    {
        [Key]
        public int ChallengeId { get; set; }

        [ForeignKey("RequestId,CAConfig")]
        public Entry Entry { get; set; }
        public string? Type { get; set; } = string.Empty;
        public string? Location { get; set; } = string.Empty;
        public string? Content { get; set; } = string.Empty;
        public string? State { get; set; } = string.Empty;
    }
}
