using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.DTOs.Velneo.Metrics;
using SegurosApp.API.Interfaces;

namespace SegurosApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VelneoMetricsController : ControllerBase
    {
        private readonly IVelneoMetricsService _metricsService;
        private readonly ILogger<VelneoMetricsController> _logger;

        public VelneoMetricsController(IVelneoMetricsService metricsService, ILogger<VelneoMetricsController> logger)
        {
            _metricsService = metricsService;
            _logger = logger;
        }

        [HttpGet("overview")]
        [ProducesResponseType(typeof(VelneoMetricsOverviewDto), 200)]
        public async Task<ActionResult<VelneoMetricsOverviewDto>> GetMetricsOverview(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool includeAllUsers = false)
        {
            try
            {
                var userId = includeAllUsers ? null : GetCurrentUserId();

                _logger.LogInformation("📊 Obteniendo métricas overview - Usuario: {UserId}, Desde: {FromDate}, Hasta: {ToDate}",
                    userId?.ToString() ?? "TODOS", fromDate?.ToString("yyyy-MM-dd") ?? "N/A", toDate?.ToString("yyyy-MM-dd") ?? "N/A");

                var metrics = await _metricsService.GetMetricsOverviewAsync(userId, fromDate, toDate);

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo métricas overview");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("operation/{operationType}")]
        [ProducesResponseType(typeof(VelneoOperationStatsDto), 200)]
        public async Task<ActionResult<VelneoOperationStatsDto>> GetOperationMetrics(
            string operationType,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool includeAllUsers = false)
        {
            try
            {
                if (string.IsNullOrEmpty(operationType) ||
                    !IsValidOperationType(operationType.ToUpper()))
                {
                    return BadRequest(new { message = "Tipo de operación inválido. Use: CREATE, MODIFY, RENEW" });
                }

                var userId = includeAllUsers ? null : GetCurrentUserId();
                var normalizedOperationType = operationType.ToUpper();

                var stats = await _metricsService.GetOperationStatsAsync(normalizedOperationType, userId, fromDate, toDate);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo métricas de operación {OperationType}", operationType);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("details")]
        [ProducesResponseType(typeof(List<VelneoMetricDetailDto>), 200)]
        public async Task<ActionResult<List<VelneoMetricDetailDto>>> GetDetailedMetrics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? operationType = null,
            [FromQuery] bool? success = null,
            [FromQuery] int? companiaId = null,
            [FromQuery] bool includeAllUsers = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                var filters = new VelneoMetricsFilters
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    OperationType = operationType?.ToUpper(),
                    Success = success,
                    CompaniaId = companiaId,
                    UserId = includeAllUsers ? null : GetCurrentUserId()
                };

                var details = await _metricsService.GetDetailedMetricsAsync(filters, page, pageSize);

                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo métricas detalladas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> GetQuickSummary([FromQuery] bool includeAllUsers = false)
        {
            try
            {
                var userId = includeAllUsers ? null : GetCurrentUserId();
                var overview = await _metricsService.GetMetricsOverviewAsync(userId);

                var summary = new
                {
                    totalOperations = overview.Global.TotalOperations,
                    successfulOperations = overview.Global.TotalSuccessful,
                    successRate = Math.Round(overview.Global.SuccessRate, 1),
                    todayOperations = overview.ByPeriod.Today.Total,
                    thisWeekOperations = overview.ByPeriod.ThisWeek.Total,
                    operationBreakdown = new
                    {
                        create = overview.ByOperation.Create.Total,
                        modify = overview.ByOperation.Modify.Total,
                        renew = overview.ByOperation.Renew.Total
                    },
                    lastUpdated = overview.GeneratedAt
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo resumen rápido");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }

        private bool IsValidOperationType(string operationType)
        {
            return operationType == "POLIZA_NUEVA" || operationType == "CAMBIO" || operationType == "RENOVACION";
        }
    }
}