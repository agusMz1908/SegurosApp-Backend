namespace SegurosApp.API.DTOs
{
    public class PaymentStatusSummaryDto
    {
        public int PaidBills { get; set; }
        public int PendingBills { get; set; }
        public int OverdueBills { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal OverdueAmount { get; set; }
    }
}
