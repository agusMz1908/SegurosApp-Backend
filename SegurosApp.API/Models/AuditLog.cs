using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SegurosApp.API.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EntityType { get; set; }

        [MaxLength(50)]
        public string? EntityId { get; set; }

        public string? Details { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}