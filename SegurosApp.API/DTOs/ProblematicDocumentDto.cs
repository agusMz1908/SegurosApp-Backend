namespace SegurosApp.API.DTOs
{
    public class ProblematicDocumentDto
    {
        public string FileName { get; set; } = string.Empty;
        public int ErrorCount { get; set; }
        public decimal AverageSuccessRate { get; set; }
        public string MostCommonError { get; set; } = string.Empty;
    }
}
