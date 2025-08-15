using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SegurosApp.API.Models
{
    [Table("DailyMetrics")]
    public class DailyMetrics
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public int TotalScans { get; set; }
        public int SuccessfulScans { get; set; }
        public int FailedScans { get; set; }
        public int BillableScans { get; set; }
        public decimal AvgProcessingTimeMs { get; set; }
        public decimal AvgSuccessRate { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}