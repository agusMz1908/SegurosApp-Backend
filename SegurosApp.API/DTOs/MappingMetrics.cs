namespace SegurosApp.API.DTOs
{
    namespace SegurosApp.API.DTOs
    {
        public class MappingMetrics
        {
            public int TotalFieldsScanned { get; set; }
            public int FieldsMappedSuccessfully { get; set; }
            public int FieldsWithIssues { get; set; }
            public int FieldsRequireAttention { get; set; }
            public decimal OverallConfidence { get; set; }
            public string MappingQuality { get; set; } = string.Empty;
            public List<string> MissingCriticalFields { get; set; } = new();
            public decimal OverallCompletionPercentage { get; set; }
            public CategoryBreakdown FieldsByCategory { get; set; } = new();
            public PerformanceMetrics Performance { get; set; } = new();
            public List<ImprovementSuggestion> Suggestions { get; set; } = new();
            public ConfidenceBreakdown Confidence { get; set; } = new();
        }

        public class CategoryBreakdown
        {
            public CategoryMetric PolicyFields { get; set; } = new();      // Datos de póliza
            public CategoryMetric VehicleFields { get; set; } = new();     // Datos de vehículo
            public CategoryMetric FinancialFields { get; set; } = new();   // Datos financieros
            public CategoryMetric ClientFields { get; set; } = new();      // Datos de cliente
            public CategoryMetric MasterDataFields { get; set; } = new();  // Campos de master data
            public CategoryMetric OptionalFields { get; set; } = new();    // Campos opcionales
        }
        public class CategoryMetric
        {
            public int TotalFields { get; set; }
            public int MappedFields { get; set; }
            public int MissingFields { get; set; }
            public decimal CompletionPercentage { get; set; }
            public decimal AverageConfidence { get; set; }
            public List<string> CriticalMissing { get; set; } = new();
            public List<string> SuccessfullyMapped { get; set; } = new();

            public string QualityLevel => CompletionPercentage switch
            {
                >= 90 => "Excelente",
                >= 75 => "Bueno",
                >= 50 => "Aceptable",
                >= 25 => "Básico",
                _ => "Insuficiente"
            };
        }
        public class PerformanceMetrics
        {
            public int ProcessingTimeMs { get; set; }
            public int ValidationTimeMs { get; set; }
            public int MasterDataLookupTimeMs { get; set; }
            public int TotalMappingTimeMs { get; set; }

            public decimal FieldsPerSecond { get; set; }
            public decimal AutoMappingSuccessRate { get; set; }
            public int ManualReviewRequired { get; set; }

            public string PerformanceLevel => TotalMappingTimeMs switch
            {
                < 1000 => "Muy Rápido",
                < 3000 => "Rápido",
                < 5000 => "Normal",
                < 10000 => "Lento",
                _ => "Muy Lento"
            };
        }

        public class ImprovementSuggestion
        {
            public string Category { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty; // High, Medium, Low
            public string ActionType { get; set; } = string.Empty; // DocumentQuality, Configuration, Training
            public List<string> SpecificFields { get; set; } = new();
            public decimal PotentialImprovement { get; set; } // Porcentaje de mejora estimado
        }

        public class ConfidenceBreakdown
        {
            public ConfidenceLevel ExactMatches { get; set; } = new();     // 90-100%
            public ConfidenceLevel HighConfidence { get; set; } = new();   // 75-89%
            public ConfidenceLevel MediumConfidence { get; set; } = new(); // 50-74%
            public ConfidenceLevel LowConfidence { get; set; } = new();    // 25-49%
            public ConfidenceLevel VeryLowConfidence { get; set; } = new(); // 0-24%

            public decimal WeightedAverageConfidence { get; set; }
            public string OverallConfidenceLevel { get; set; } = string.Empty;
        }

        public class ConfidenceLevel
        {
            public int FieldCount { get; set; }
            public decimal Percentage { get; set; }
            public List<string> FieldNames { get; set; } = new();
            public decimal AverageConfidence { get; set; }
        }

        public class DetailedFieldMetric
        {
            public string FieldName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public bool IsCritical { get; set; }
            public bool IsMapped { get; set; }
            public decimal Confidence { get; set; }
            public string SourceType { get; set; } = string.Empty; // Direct, Inferred, MasterData, Default
            public string RawValue { get; set; } = string.Empty;
            public string MappedValue { get; set; } = string.Empty;
            public string ValidationStatus { get; set; } = string.Empty; // Valid, Warning, Error
            public List<string> ValidationMessages { get; set; } = new();

            public bool RequiresAttention =>
                !IsMapped ||
                Confidence < 0.7m ||
                ValidationStatus == "Error" ||
                (IsCritical && ValidationStatus == "Warning");
        }

        public class CompleteMappingReport
        {
            public MappingMetrics Summary { get; set; } = new();
            public List<DetailedFieldMetric> DetailedFields { get; set; } = new();
            public Dictionary<string, object> RawExtractedData { get; set; } = new();
            public Dictionary<string, object> ProcessedData { get; set; } = new();
            public List<MappingStep> ProcessingSteps { get; set; } = new();
            public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
            public string DocumentFileName { get; set; } = string.Empty;
            public int ScanId { get; set; }
        }

        public class MappingStep
        {
            public int StepNumber { get; set; }
            public string StepName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int DurationMs { get; set; }
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public Dictionary<string, object> StepData { get; set; } = new();

            public int FieldsProcessed { get; set; }
            public int FieldsSuccessful { get; set; }
            public decimal StepSuccessRate => FieldsProcessed > 0 ?
                (decimal)FieldsSuccessful / FieldsProcessed * 100 : 0;
        }
    }
}
