using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Metrics;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Models;
using System.Text.Json;

namespace SegurosApp.API.Services
{
    public class VelneoMetricsService : IVelneoMetricsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VelneoMetricsService> _logger;

        public VelneoMetricsService(AppDbContext context, ILogger<VelneoMetricsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RecordOperationAsync(VelneoOperationMetric metric)
        {
            try
            {
                _context.VelneoOperationMetrics.Add(metric);
                await _context.SaveChangesAsync();

                _logger.LogInformation("📊 Métrica registrada: {OperationType} - {Success} - Usuario: {UserId}",
                    metric.OperationType, metric.Success ? "SUCCESS" : "FAILED", metric.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error registrando métrica de operación");
            }
        }

        public async Task<VelneoMetricsOverviewDto> GetMetricsOverviewAsync(int? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.VelneoOperationMetrics.AsQueryable();

            if (userId.HasValue)
                query = query.Where(m => m.UserId == userId.Value);

            if (fromDate.HasValue)
                query = query.Where(m => m.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(m => m.CreatedAt <= toDate.Value);

            var metrics = await query.ToListAsync();

            var overview = new VelneoMetricsOverviewDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                Global = CalculateGlobalMetrics(metrics, fromDate, toDate),
                ByOperation = CalculateOperationMetrics(metrics),
                ByPeriod = CalculatePeriodMetrics(metrics)
            };

            return overview;
        }

        public async Task<List<VelneoMetricDetailDto>> GetDetailedMetricsAsync(VelneoMetricsFilters filters, int page = 1, int pageSize = 50)
        {
            var query = _context.VelneoOperationMetrics
                .Include(m => m.User)
                .AsQueryable();

            if (filters.FromDate.HasValue)
                query = query.Where(m => m.CreatedAt >= filters.FromDate.Value);

            if (filters.ToDate.HasValue)
                query = query.Where(m => m.CreatedAt <= filters.ToDate.Value);

            if (!string.IsNullOrEmpty(filters.OperationType))
                query = query.Where(m => m.OperationType == filters.OperationType);

            if (filters.Success.HasValue)
                query = query.Where(m => m.Success == filters.Success.Value);

            if (filters.UserId.HasValue)
                query = query.Where(m => m.UserId == filters.UserId.Value);

            if (filters.CompaniaId.HasValue)
                query = query.Where(m => m.CompaniaId == filters.CompaniaId.Value);

            var results = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new VelneoMetricDetailDto
                {
                    Id = m.Id,
                    OperationType = m.OperationType,
                    ScanId = m.ScanId,
                    VelneoPolizaId = m.VelneoPolizaId,
                    PolizaNumber = m.PolizaNumber,
                    PolizaAnteriorId = m.PolizaAnteriorId,
                    Success = m.Success,
                    ErrorMessage = m.ErrorMessage,
                    DurationMs = m.DurationMs,
                    CreatedAt = m.CreatedAt,
                    CompaniaId = m.CompaniaId,
                    UserName = m.User != null ? m.User.Username : null
                })
                .ToListAsync();

            return results;
        }

        public async Task<VelneoOperationStatsDto> GetOperationStatsAsync(string operationType, int? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.VelneoOperationMetrics
                .Where(m => m.OperationType == operationType);

            if (userId.HasValue)
                query = query.Where(m => m.UserId == userId.Value);

            if (fromDate.HasValue)
                query = query.Where(m => m.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(m => m.CreatedAt <= toDate.Value);

            var metrics = await query.ToListAsync();

            return CalculateStats(metrics);
        }

        private VelneoGlobalMetricsDto CalculateGlobalMetrics(List<VelneoOperationMetric> metrics, DateTime? fromDate, DateTime? toDate)
        {
            var successful = metrics.Count(m => m.Success);
            var total = metrics.Count;
            var failed = total - successful;

            var avgDuration = metrics.Where(m => m.DurationMs.HasValue).Average(m => m.DurationMs) ?? 0;

            // Calcular operaciones por día
            var operationsPerDay = 0m;
            if (fromDate.HasValue && toDate.HasValue && total > 0)
            {
                var days = (toDate.Value - fromDate.Value).Days;
                if (days > 0)
                    operationsPerDay = (decimal)total / days;
            }

            return new VelneoGlobalMetricsDto
            {
                TotalSuccessful = successful,
                TotalFailed = failed,
                TotalOperations = total,
                SuccessRate = total > 0 ? (decimal)successful / total * 100 : 0,
                AverageDurationMs = avgDuration,
                OperationsPerDay = operationsPerDay
            };
        }

        private VelneoOperationMetricsDto CalculateOperationMetrics(List<VelneoOperationMetric> metrics)
        {
            return new VelneoOperationMetricsDto
            {
                Create = CalculateStats(metrics.Where(m => m.OperationType == VelneoOperationType.CREATE).ToList()),
                Modify = CalculateStats(metrics.Where(m => m.OperationType == VelneoOperationType.MODIFY).ToList()),
                Renew = CalculateStats(metrics.Where(m => m.OperationType == VelneoOperationType.RENEW).ToList())
            };
        }

        private VelneoPeriodMetricsDto CalculatePeriodMetrics(List<VelneoOperationMetric> allMetrics)
        {
            var now = DateTime.Now;

            return new VelneoPeriodMetricsDto
            {
                Today = CalculateStats(allMetrics.Where(m => m.CreatedAt.Date == now.Date).ToList()),
                ThisWeek = CalculateStats(allMetrics.Where(m => GetWeekStart(m.CreatedAt) == GetWeekStart(now)).ToList()),
                ThisMonth = CalculateStats(allMetrics.Where(m => m.CreatedAt.Year == now.Year && m.CreatedAt.Month == now.Month).ToList()),
                Last24Hours = CalculateStats(allMetrics.Where(m => m.CreatedAt >= now.AddHours(-24)).ToList()),
                Last7Days = CalculateStats(allMetrics.Where(m => m.CreatedAt >= now.AddDays(-7)).ToList()),
                Last30Days = CalculateStats(allMetrics.Where(m => m.CreatedAt >= now.AddDays(-30)).ToList())
            };
        }

        private VelneoOperationStatsDto CalculateStats(List<VelneoOperationMetric> metrics)
        {
            var successful = metrics.Count(m => m.Success);
            var total = metrics.Count;
            var failed = total - successful;
            var avgDuration = metrics.Where(m => m.DurationMs.HasValue).Average(m => m.DurationMs) ?? 0;

            return new VelneoOperationStatsDto
            {
                Successful = successful,
                Failed = failed,
                Total = total,
                SuccessRate = total > 0 ? (decimal)successful / total * 100 : 0,
                AverageDurationMs = avgDuration
            };
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }
}