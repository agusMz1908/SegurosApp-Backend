namespace SegurosApp.API.Models
{
    public class DailyMetrics
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }

        public int TotalScans { get; set; } = 0;
        public int SuccessfulScans { get; set; } = 0;
        public int BillableScans { get; set; } = 0;
        public int FailedScans { get; set; } = 0;
        public int PolizasCreated { get; set; } = 0;

        public int AvgProcessingTimeMs { get; set; } = 0;
        public decimal AvgSuccessRate { get; set; } = 0;
        public decimal EstimatedRevenue { get; set; } = 0;

        // Navigation properties
        public User User { get; set; } = null!;
    }
}