using Azure.AI.DocumentIntelligence;
using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CompanyAddress { get; set; }

        [MaxLength(50)]
        public string? CompanyRUC { get; set; }

        [MaxLength(200)]
        public string? ContactPerson { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public List<DocumentScan> DocumentScans { get; set; } = new();
        public List<DailyMetrics> DailyMetrics { get; set; } = new();
        public List<MonthlyBilling> MonthlyBillings { get; set; } = new();
    }
}