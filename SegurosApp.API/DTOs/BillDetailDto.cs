namespace SegurosApp.API.DTOs
{
    public class BillDetailDto : MonthlyBillingDto
    {
        public List<BillingItemDto> BillingItems { get; set; } = new();
    }

    public class BillingItemDto
    {
        public int Id { get; set; }
        public DateTime ScanDate { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? VelneoPolizaNumber { get; set; }
        public decimal PricePerPoliza { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
