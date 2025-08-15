namespace SegurosApp.API.DTOs
{
    public class DocumentHistoryDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal SuccessRate { get; set; }
        public int FieldsExtracted { get; set; }
        public int ProcessingTimeMs { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Info de Velneo
        public string? VelneoPolizaNumber { get; set; }
        public bool VelneoCreated { get; set; }

        // Facturación
        public bool IsBillable { get; set; }
        public bool IsBilled { get; set; }
        public DateTime? BilledAt { get; set; }
    }
}
