namespace SegurosApp.API.DTOs.Velneo.Metrics
{
    public class VelneoMetricDetailDto
    {
        public int Id { get; set; }
        public string OperationType { get; set; } = "";
        public int? ScanId { get; set; }
        public int? VelneoPolizaId { get; set; }
        public string? PolizaNumber { get; set; }
        public int? PolizaAnteriorId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public long? DurationMs { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CompaniaId { get; set; }
        public string? UserName { get; set; }
    }
}
