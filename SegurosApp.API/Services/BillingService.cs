using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.Models;

namespace SegurosApp.API.Services
{
    public class BillingService
    {
        private readonly AppDbContext _context;
        private readonly PricingService _pricingService;
        private readonly ILogger<BillingService> _logger;

        public BillingService(
            AppDbContext context,
            PricingService pricingService,
            ILogger<BillingService> logger)
        {
            _context = context;
            _pricingService = pricingService;
            _logger = logger;
        }

        public async Task<BillingStatsDto> GetCurrentMonthStatsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var polizasCount = await _context.DocumentScans
                    .Where(ds => ds.CreatedAt >= startOfMonth &&
                                ds.CreatedAt <= endOfMonth &&
                                ds.VelneoCreated &&
                                !string.IsNullOrEmpty(ds.VelneoPolizaNumber) &&
                                ds.IsBillable &&
                                !ds.IsBilled)
                    .CountAsync();

                var tier = polizasCount > 0
                    ? await _pricingService.GetApplicableTierAsync(polizasCount)
                    : null;

                var estimatedCost = tier != null ? tier.PricePerPoliza * polizasCount : 0;

                var lastBilling = await _context.MonthlyBilling
                    .OrderByDescending(mb => mb.BillingYear)
                    .ThenByDescending(mb => mb.BillingMonth)
                    .FirstOrDefaultAsync();

                var result = new BillingStatsDto
                {
                    TotalPolizasThisMonth = polizasCount,
                    EstimatedCost = estimatedCost,
                    ApplicableTierName = tier?.TierName ?? "Sin tier aplicable",
                    PricePerPoliza = tier?.PricePerPoliza ?? 0,
                    DaysLeftInMonth = (endOfMonth - now).Days + 1,
                    LastBillingDate = lastBilling?.GeneratedAt ?? DateTime.MinValue
                };

                _logger.LogInformation("📊 Stats mes actual: {PolizasCount} pólizas, tier '{TierName}', costo estimado ${EstimatedCost}",
                    polizasCount, result.ApplicableTierName, estimatedCost);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo estadísticas del mes actual");
                throw;
            }
        }
        public async Task<MonthlyBillingDto> GenerateMonthlyBillAsync(int year, int month, string companyName, string? companyAddress = null, string? companyRUC = null)
        {
            try
            {
                _logger.LogInformation("🔄 Generando factura mensual para {Year}/{Month}", year, month);
                var existingBill = await _context.MonthlyBilling
                    .FirstOrDefaultAsync(mb => mb.BillingYear == year && mb.BillingMonth == month);

                if (existingBill != null)
                {
                    throw new InvalidOperationException($"Ya existe una factura para {month}/{year}");
                }

                var startOfMonth = new DateTime(year, month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var successfulScans = await _context.DocumentScans
                    .Where(ds => ds.CreatedAt >= startOfMonth &&
                                ds.CreatedAt <= endOfMonth &&
                                ds.VelneoCreated &&
                                !string.IsNullOrEmpty(ds.VelneoPolizaNumber) &&
                                ds.IsBillable &&
                                !ds.IsBilled)
                    .ToListAsync();

                if (successfulScans.Count == 0)
                {
                    _logger.LogWarning("⚠️ No hay pólizas exitosas para facturar en {Month}/{Year}", month, year);
                    throw new InvalidOperationException($"No hay pólizas para facturar en {month}/{year}");
                }

                var tier = await _pricingService.GetApplicableTierAsync(successfulScans.Count);

                var subTotal = tier.PricePerPoliza * successfulScans.Count;
                var taxAmount = 0m; 
                var totalAmount = subTotal + taxAmount;

                var monthlyBill = new MonthlyBilling
                {
                    UserId = 1, 
                    BillingYear = year,
                    BillingMonth = month,
                    TotalPolizasEscaneadas = successfulScans.Count,
                    TotalBillableScans = successfulScans.Count,
                    AppliedTierId = tier.Id,
                    PricePerPoliza = tier.PricePerPoliza,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    GeneratedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30), 
                    CompanyName = companyName,
                    CompanyAddress = companyAddress,
                    CompanyRUC = companyRUC
                };

                _context.MonthlyBilling.Add(monthlyBill);
                await _context.SaveChangesAsync();

                var billingItems = successfulScans.Select(scan => new BillingItems
                {
                    MonthlyBillingId = monthlyBill.Id,
                    DocumentScanId = scan.Id,
                    ScanDate = scan.CreatedAt, 
                    FileName = scan.FileName,
                    VelneoPolizaNumber = scan.VelneoPolizaNumber, 
                    PricePerPoliza = tier.PricePerPoliza,
                    Amount = tier.PricePerPoliza,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.BillingItems.AddRange(billingItems);

                foreach (var scan in successfulScans)
                {
                    scan.IsBilled = true;
                    scan.BilledAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Factura mensual generada: ID={BillId}, {PolizasCount} pólizas, total=${TotalAmount}",
                    monthlyBill.Id, successfulScans.Count, totalAmount);

                return new MonthlyBillingDto
                {
                    Id = monthlyBill.Id,
                    BillingYear = monthlyBill.BillingYear,
                    BillingMonth = monthlyBill.BillingMonth,
                    TotalPolizasEscaneadas = monthlyBill.TotalPolizasEscaneadas,
                    AppliedTierName = tier.TierName,
                    PricePerPoliza = monthlyBill.PricePerPoliza,
                    SubTotal = monthlyBill.SubTotal,
                    TaxAmount = monthlyBill.TaxAmount,
                    TotalAmount = monthlyBill.TotalAmount,
                    Status = monthlyBill.Status,
                    GeneratedAt = monthlyBill.GeneratedAt,
                    DueDate = monthlyBill.DueDate,
                    CompanyName = monthlyBill.CompanyName,
                    CompanyAddress = monthlyBill.CompanyAddress,
                    CompanyRUC = monthlyBill.CompanyRUC,
                    BillingItemsCount = billingItems.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando factura mensual para {Year}/{Month}", year, month);
                throw;
            }
        }

        public async Task<List<MonthlyBillingDto>> GetCompanyBillsAsync()
        {
            try
            {
                var bills = await _context.MonthlyBilling
                    .Include(mb => mb.AppliedTier)
                    .Include(mb => mb.BillingItems)
                    .OrderByDescending(mb => mb.BillingYear)
                    .ThenByDescending(mb => mb.BillingMonth)
                    .ToListAsync();

                var result = bills.Select(bill => new MonthlyBillingDto
                {
                    Id = bill.Id,
                    BillingYear = bill.BillingYear,
                    BillingMonth = bill.BillingMonth,
                    TotalPolizasEscaneadas = bill.TotalPolizasEscaneadas,
                    AppliedTierName = bill.AppliedTier.TierName,
                    PricePerPoliza = bill.PricePerPoliza,
                    SubTotal = bill.SubTotal,
                    TaxAmount = bill.TaxAmount,
                    TotalAmount = bill.TotalAmount,
                    Status = bill.Status,
                    GeneratedAt = bill.GeneratedAt,
                    DueDate = bill.DueDate,
                    PaidAt = bill.PaidAt,
                    PaymentMethod = bill.PaymentMethod,
                    PaymentReference = bill.PaymentReference,
                    CompanyName = bill.CompanyName,
                    CompanyAddress = bill.CompanyAddress,
                    CompanyRUC = bill.CompanyRUC,
                    BillingItemsCount = bill.BillingItems.Count
                }).ToList();

                _logger.LogInformation("📄 Obtenidas {Count} facturas de la empresa", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo facturas de la empresa");
                throw;
            }
        }
        public async Task<bool> MarkBillAsPaidAsync(int billId, string paymentMethod, string? paymentReference = null)
        {
            try
            {
                var bill = await _context.MonthlyBilling.FindAsync(billId);
                if (bill == null)
                {
                    return false;
                }

                bill.Status = "Paid";
                bill.PaidAt = DateTime.UtcNow;
                bill.PaymentMethod = paymentMethod;
                bill.PaymentReference = paymentReference;

                await _context.SaveChangesAsync();

                _logger.LogInformation("💳 Factura {BillId} marcada como pagada: {PaymentMethod}", billId, paymentMethod);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error marcando factura {BillId} como pagada", billId);
                throw;
            }
        }

        public async Task<BillDetailDto?> GetBillDetailAsync(int billId)
        {
            try
            {
                var bill = await _context.MonthlyBilling
                    .Include(mb => mb.AppliedTier)
                    .Include(mb => mb.BillingItems)
                        .ThenInclude(bi => bi.DocumentScan)
                    .FirstOrDefaultAsync(mb => mb.Id == billId);

                if (bill == null)
                {
                    _logger.LogWarning("Factura con ID {BillId} no encontrada", billId);
                    return null;
                }

                var result = new BillDetailDto
                {
                    Id = bill.Id,
                    BillingYear = bill.BillingYear,
                    BillingMonth = bill.BillingMonth,
                    TotalPolizasEscaneadas = bill.TotalPolizasEscaneadas,
                    AppliedTierName = bill.AppliedTier.TierName,
                    PricePerPoliza = bill.PricePerPoliza,
                    SubTotal = bill.SubTotal,
                    TaxAmount = bill.TaxAmount,
                    TotalAmount = bill.TotalAmount,
                    Status = bill.Status,
                    GeneratedAt = bill.GeneratedAt,
                    DueDate = bill.DueDate,
                    PaidAt = bill.PaidAt,
                    PaymentMethod = bill.PaymentMethod,
                    PaymentReference = bill.PaymentReference,
                    CompanyName = bill.CompanyName,
                    CompanyAddress = bill.CompanyAddress,
                    CompanyRUC = bill.CompanyRUC,
                    BillingItemsCount = bill.BillingItems.Count,
                    BillingItems = bill.BillingItems.Select(bi => new BillingItemDto
                    {
                        Id = bi.Id,
                        ScanDate = bi.ScanDate,
                        FileName = bi.FileName,
                        VelneoPolizaNumber = bi.VelneoPolizaNumber,
                        PricePerPoliza = bi.PricePerPoliza,
                        Amount = bi.Amount,
                        CreatedAt = bi.CreatedAt
                    }).OrderBy(bi => bi.ScanDate).ToList()
                };

                _logger.LogInformation("Obtenido detalle de factura {BillId} con {ItemsCount} items",
                    billId, result.BillingItems.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo detalle de factura {BillId}", billId);
                throw;
            }
        }

        public async Task<MonthlyBillingSummaryDto?> GetMonthlySummaryAsync(int year, int month)
        {
            try
            {
                var bills = await _context.MonthlyBilling
                    .Include(mb => mb.AppliedTier)
                    .Include(mb => mb.BillingItems)
                    .Where(mb => mb.BillingYear == year && mb.BillingMonth == month)
                    .ToListAsync();

                if (!bills.Any())
                {
                    return null;
                }

                var totalCompanies = bills.Count;
                var totalPolizas = bills.Sum(b => b.TotalPolizasEscaneadas);
                var totalRevenue = bills.Sum(b => b.TotalAmount);

                var tierUsage = bills
                    .GroupBy(b => new { b.AppliedTier.TierName, b.AppliedTier.PricePerPoliza })
                    .Select(g => new TierUsageSummaryDto
                    {
                        TierName = g.Key.TierName,
                        CompaniesCount = g.Count(),
                        TotalPolizas = g.Sum(b => b.TotalPolizasEscaneadas),
                        TotalRevenue = g.Sum(b => b.TotalAmount),
                        PricePerPoliza = g.Key.PricePerPoliza
                    }).ToList();

                var paidBills = bills.Where(b => b.Status == "Paid").ToList();
                var pendingBills = bills.Where(b => b.Status == "Pending" && b.DueDate >= DateTime.UtcNow).ToList();
                var overdueBills = bills.Where(b => b.Status == "Pending" && b.DueDate < DateTime.UtcNow).ToList();

                var paymentStatus = new PaymentStatusSummaryDto
                {
                    PaidBills = paidBills.Count,
                    PendingBills = pendingBills.Count,
                    OverdueBills = overdueBills.Count,
                    PaidAmount = paidBills.Sum(b => b.TotalAmount),
                    PendingAmount = pendingBills.Sum(b => b.TotalAmount),
                    OverdueAmount = overdueBills.Sum(b => b.TotalAmount)
                };

                var summary = new MonthlyBillingSummaryDto
                {
                    Year = year,
                    Month = month,
                    MonthName = new DateTime(year, month, 1).ToString("MMMM"),
                    TotalCompanies = totalCompanies,
                    TotalPolizasEscaneadas = totalPolizas,
                    TotalRevenue = totalRevenue,
                    AverageRevenuePerCompany = totalCompanies > 0 ? totalRevenue / totalCompanies : 0,
                    AveragePolizasPerCompany = totalCompanies > 0 ? (decimal)totalPolizas / totalCompanies : 0,
                    TierUsage = tierUsage,
                    PaymentStatus = paymentStatus,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Resumen mensual {Month}/{Year}: {Companies} empresas, ${Revenue} ingresos",
                    month, year, totalCompanies, totalRevenue);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando resumen mensual {Month}/{Year}", month, year);
                throw;
            }
        }

        public async Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(int months)
        {
            try
            {
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddMonths(-months);

                // ✅ FIX: Calcular año y mes mínimos para el filtro en lugar de usar DateTime constructor
                var minYear = startDate.Year;
                var minMonth = startDate.Month;

                var bills = await _context.MonthlyBilling
                    .Include(mb => mb.AppliedTier)
                    .Where(mb => mb.BillingYear > minYear ||
                                (mb.BillingYear == minYear && mb.BillingMonth >= minMonth))
                    .ToListAsync();

                // Revenue mensual
                var monthlyRevenue = bills
                    .GroupBy(b => new { b.BillingYear, b.BillingMonth })
                    .Select(g => new MonthlyRevenueDto
                    {
                        Year = g.Key.BillingYear,
                        Month = g.Key.BillingMonth,
                        MonthName = new DateTime(g.Key.BillingYear, g.Key.BillingMonth, 1).ToString("MMM yyyy"),
                        Revenue = g.Sum(b => b.TotalAmount),
                        CompaniesCount = g.Count(),
                        PolizasCount = g.Sum(b => b.TotalPolizasEscaneadas),
                        AverageRevenuePerCompany = g.Average(b => b.TotalAmount)
                    })
                    .OrderBy(mr => mr.Year)
                    .ThenBy(mr => mr.Month)
                    .ToList();

                // Métricas totales
                var totalMetrics = new RevenueMetricsDto
                {
                    TotalRevenue = bills.Sum(b => b.TotalAmount),
                    AverageMonthlyRevenue = monthlyRevenue.Any() ? monthlyRevenue.Average(mr => mr.Revenue) : 0,
                    HighestMonthRevenue = monthlyRevenue.Any() ? monthlyRevenue.Max(mr => mr.Revenue) : 0,
                    LowestMonthRevenue = monthlyRevenue.Any() ? monthlyRevenue.Min(mr => mr.Revenue) : 0,
                    TotalCompaniesServed = bills.Select(b => b.CompanyName).Distinct().Count(),
                    TotalPolizasProcessed = bills.Sum(b => b.TotalPolizasEscaneadas)
                };

                // Performance por tier
                var tierPerformance = bills
                    .GroupBy(b => new { b.AppliedTier.TierName, b.AppliedTier.PricePerPoliza, b.AppliedTier.MinPolizas, b.AppliedTier.MaxPolizas })
                    .Select(g => new TierPerformanceDto
                    {
                        TierName = g.Key.TierName,
                        PricePerPoliza = g.Key.PricePerPoliza,
                        TotalUsage = g.Count(),
                        TotalRevenue = g.Sum(b => b.TotalAmount),
                        MinPolizas = g.Key.MinPolizas,
                        MaxPolizas = g.Key.MaxPolizas ?? int.MaxValue
                    })
                    .ToList();

                // Calcular porcentajes
                var totalTierRevenue = tierPerformance.Sum(tp => tp.TotalRevenue);
                tierPerformance.ForEach(tp =>
                    tp.RevenuePercentage = totalTierRevenue > 0 ? (tp.TotalRevenue / totalTierRevenue) * 100 : 0);

                // Análisis de crecimiento
                var growthAnalysis = CalculateGrowthAnalysis(monthlyRevenue);

                var analytics = new RevenueAnalyticsDto
                {
                    MonthlyRevenue = monthlyRevenue,
                    TotalMetrics = totalMetrics,
                    TierPerformance = tierPerformance,
                    GrowthAnalysis = growthAnalysis,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("📈 Analytics generado: ${TotalRevenue} ingresos totales en {Months} meses",
                    totalMetrics.TotalRevenue, months);

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando analytics de ingresos");
                throw;
            }
        }

        private GrowthAnalysisDto CalculateGrowthAnalysis(List<MonthlyRevenueDto> monthlyRevenue)
        {
            if (monthlyRevenue.Count < 2)
            {
                return new GrowthAnalysisDto { GrowthTrend = "insufficient_data" };
            }

            var sortedRevenue = monthlyRevenue.OrderBy(mr => mr.Year).ThenBy(mr => mr.Month).ToList();
            var lastMonth = sortedRevenue.Last();
            var previousMonth = sortedRevenue[sortedRevenue.Count - 2];

            var monthOverMonthGrowth = previousMonth.Revenue != 0
                ? ((lastMonth.Revenue - previousMonth.Revenue) / previousMonth.Revenue) * 100
                : 0;

            var lastYearSameMonth = sortedRevenue.FirstOrDefault(mr =>
                mr.Year == lastMonth.Year - 1 && mr.Month == lastMonth.Month);

            var yearOverYearGrowth = lastYearSameMonth?.Revenue != 0
                ? ((lastMonth.Revenue - (lastYearSameMonth?.Revenue ?? 0)) / (lastYearSameMonth?.Revenue ?? 1)) * 100
                : 0;

            var trend = "stable";
            if (sortedRevenue.Count >= 3)
            {
                var lastThreeMonths = sortedRevenue.TakeLast(3).ToList();
                var isIncreasing = lastThreeMonths[2].Revenue > lastThreeMonths[1].Revenue &&
                                  lastThreeMonths[1].Revenue > lastThreeMonths[0].Revenue;
                var isDecreasing = lastThreeMonths[2].Revenue < lastThreeMonths[1].Revenue &&
                                  lastThreeMonths[1].Revenue < lastThreeMonths[0].Revenue;

                trend = isIncreasing ? "increasing" : isDecreasing ? "decreasing" : "stable";
            }

            var predictedNextMonth = sortedRevenue.Count >= 3
                ? sortedRevenue.TakeLast(3).Average(mr => mr.Revenue)
                : lastMonth.Revenue;

            return new GrowthAnalysisDto
            {
                MonthOverMonthGrowth = monthOverMonthGrowth,
                YearOverYearGrowth = yearOverYearGrowth,
                GrowthTrend = trend,
                PredictedNextMonth = predictedNextMonth
            };
        }
    }
}