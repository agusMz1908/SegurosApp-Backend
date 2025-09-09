using SegurosApp.API.DTOs.Velneo.Metrics;
using SegurosApp.API.Models;

namespace SegurosApp.API.Interfaces
{
    public interface IVelneoMetricsService
    {
        Task RecordOperationAsync(VelneoOperationMetric metric);
        Task<VelneoMetricsOverviewDto> GetMetricsOverviewAsync(int? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<VelneoMetricDetailDto>> GetDetailedMetricsAsync(VelneoMetricsFilters filters, int page = 1, int pageSize = 50);
        Task<VelneoOperationStatsDto> GetOperationStatsAsync(string operationType, int? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
