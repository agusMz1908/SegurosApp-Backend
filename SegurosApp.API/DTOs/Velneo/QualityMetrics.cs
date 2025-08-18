namespace SegurosApp.API.DTOs.Velneo
{
    public class QualityMetrics
    {
        public decimal AverageDataCompleteness { get; set; }
        public decimal AverageMappingConfidence { get; set; }
        public int DocumentsRequiringManualReview { get; set; }
        public int AutoProcessedDocuments { get; set; }
        public List<FieldQualityMetric> ProblematicFields { get; set; } = new();
        public List<FieldQualityMetric> BestPerformingFields { get; set; } = new();
    }
}
