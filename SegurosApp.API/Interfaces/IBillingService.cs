using SegurosApp.API.DTOs;
using SegurosApp.API.Models;

namespace SegurosApp.API.Services
{
    public interface IBillingService
    {
        Task<BillingStatsDto> GetCurrentMonthStatsAsync();
        Task<MonthlyBillingDto> GenerateMonthlyBillAsync(int year, int month, string companyName,
            string? companyAddress = null, string? companyRUC = null);
        Task<List<MonthlyBillingDto>> GetCompanyBillsAsync();
        Task<BillDetailDto?> GetBillDetailAsync(int billId);
        Task<bool> MarkBillAsPaidAsync(int billId, string paymentMethod, string? paymentReference = null);
        Task<MonthlyBillingSummaryDto?> GetMonthlySummaryAsync(int year, int month);
        Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(int months);
        Task<BillingItems?> AddToCurrentMonthBillingAsync(int scanId, int userId);
        Task ProcessMonthlyClosureAsync();
    }
}