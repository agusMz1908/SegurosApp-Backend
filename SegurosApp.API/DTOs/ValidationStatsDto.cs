namespace SegurosApp.API.DTOs
{
    public class ValidationStatsDto
    {
        public int TotalValidations { get; set; }
        public int SuccessfulValidations { get; set; }
        public int FailedValidations { get; set; }
        public int ClienteValidationFailures { get; set; }
        public int CompaniaValidationFailures { get; set; }
        public int SeccionValidationFailures { get; set; }
        public int ConnectivityIssues { get; set; }
        public int AverageValidationTimeMs { get; set; }

        public decimal SuccessRate => TotalValidations > 0 ?
            (decimal)SuccessfulValidations / TotalValidations * 100 : 0;

        public string QualityLevel => SuccessRate switch
        {
            >= 95 => "Excelente",
            >= 90 => "Muy Bueno",
            >= 80 => "Bueno",
            >= 70 => "Aceptable",
            _ => "Necesita Mejora"
        };
    }
}
