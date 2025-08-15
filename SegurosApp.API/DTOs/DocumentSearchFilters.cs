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

        // ✅ CORREGIDO: Tipos consistentes
        public int Page { get; set; } = 0;
        public int Limit { get; set; } = 20;
        public int? PageSize { get; set; }  // Alias opcional para Limit
    }
}