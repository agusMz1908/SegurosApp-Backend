namespace SegurosApp.API.DTOs
{
    public class DocumentSearchFilters
    {
        public string? FileName { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsBillable { get; set; }
        public bool? VelneoCreated { get; set; }
        public decimal? MinSuccessRate { get; set; }

        // Paginación
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
    }
}
