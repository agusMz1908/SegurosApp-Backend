namespace SegurosApp.API.DTOs
{
    public class MonthlyRevenueDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int CompaniesCount { get; set; }
        public int PolizasCount { get; set; }
        public decimal AverageRevenuePerCompany { get; set; }
    }
}