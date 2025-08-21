namespace SegurosApp.API.DTOs
{
    public class MonthlyBillingDto
    {
        public int Id { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public int TotalPolizasEscaneadas { get; set; }
        public string AppliedTierName { get; set; } = string.Empty;
        public decimal PricePerPoliza { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyAddress { get; set; }
        public string? CompanyRUC { get; set; }
        public int BillingItemsCount { get; set; }

        public string BillingPeriod => $"{BillingMonth:D2}/{BillingYear}";
        public bool IsOverdue => Status == "Pending" && DateTime.UtcNow > DueDate;
        public int DaysUntilDue => Status == "Pending" ? (DueDate - DateTime.UtcNow).Days : 0;
    }
}