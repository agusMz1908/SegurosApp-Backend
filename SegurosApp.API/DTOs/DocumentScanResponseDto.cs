namespace SegurosApp.API.DTOs
{
    public class DocumentScanResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        // Info del archivo
        public int ScanId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileMd5Hash { get; set; } = string.Empty;

        // Métricas del procesamiento
        public int ProcessingTimeMs { get; set; }
        public decimal SuccessRate { get; set; }
        public int FieldsExtracted { get; set; }
        public int TotalFieldsAttempted { get; set; }
        public string Status { get; set; } = string.Empty;

        // Datos extraídos
        public Dictionary<string, object> ExtractedData { get; set; } = new();

        // Info de duplicados
        public bool IsDuplicate { get; set; }
        public int? ExistingScanId { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
