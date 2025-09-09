namespace SegurosApp.API.DTOs.Velneo.Metrics
{
    public class VelneoMetricsFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? OperationType { get; set; }
        public bool? Success { get; set; }
        public int? UserId { get; set; }
        public int? CompaniaId { get; set; }
    }
}
