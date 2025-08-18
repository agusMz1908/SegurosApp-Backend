namespace SegurosApp.API.DTOs.Velneo
{
    public class DailyVelneoMetric
    {
        public DateTime Date { get; set; }
        public int TotalScans { get; set; }
        public int VelneoCreated { get; set; }
        public int VelneoFailed { get; set; }
        public int PendingVelneo { get; set; }
        public decimal SuccessRate { get; set; }
        public int AverageProcessingTimeMs { get; set; }
    }
}
