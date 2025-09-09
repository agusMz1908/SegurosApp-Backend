using System.ComponentModel.DataAnnotations;

namespace SegurosApp.API.Models
{
    public class VelneoOperationMetric
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        [MaxLength(20)]
        public string OperationType { get; set; } = "";
        public int? ScanId { get; set; }
        public int? VelneoPolizaId { get; set; }
        [MaxLength(50)]
        public string? PolizaNumber { get; set; }
        public int? PolizaAnteriorId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public long? DurationMs { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CompaniaId { get; set; }
        public string? AdditionalData { get; set; }
        public User? User { get; set; }
    }

    public static class VelneoOperationType
    {
        public const string CREATE = "CREATE";
        public const string MODIFY = "MODIFY";
        public const string RENEW = "RENEW";
    }
}