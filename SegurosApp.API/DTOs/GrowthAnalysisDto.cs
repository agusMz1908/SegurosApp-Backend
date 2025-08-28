namespace SegurosApp.API.DTOs
{
    public class GrowthAnalysisDto
    {
        public decimal MonthOverMonthGrowth { get; set; }
        public decimal YearOverYearGrowth { get; set; }
        public string GrowthTrend { get; set; } = string.Empty; 
        public decimal PredictedNextMonth { get; set; }
    }
}
