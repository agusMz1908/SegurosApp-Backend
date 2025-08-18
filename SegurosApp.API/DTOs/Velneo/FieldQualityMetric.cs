namespace SegurosApp.API.DTOs.Velneo
{
    public class FieldQualityMetric
    {
        public string FieldName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal ExtractionSuccessRate { get; set; }
        public decimal MappingSuccessRate { get; set; }
        public int TotalAttempts { get; set; }
        public int SuccessfulExtractions { get; set; }
        public int SuccessfulMappings { get; set; }

        public string QualityLevel => ExtractionSuccessRate switch
        {
            >= 0.9m => "Excelente",
            >= 0.7m => "Bueno",
            >= 0.5m => "Aceptable",
            >= 0.3m => "Mejorable",
            _ => "Problemático"
        };
    }
}
