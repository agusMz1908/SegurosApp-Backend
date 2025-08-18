using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo;
using SegurosApp.API.Interfaces;
using System.Security.Claims;

namespace SegurosApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IAzureDocumentService _documentService;
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IAzureDocumentService documentService,
            IVelneoMasterDataService masterDataService,
            ILogger<DashboardController> logger)
        {
            _documentService = documentService;
            _masterDataService = masterDataService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<CompleteDashboardDto>), 200)]
        public async Task<ActionResult<ApiResponse<CompleteDashboardDto>>> GetDashboard(
            [FromQuery] int days = 30)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<CompleteDashboardDto>.ErrorResult("Usuario no autenticado"));
                }

                _logger.LogInformation("📊 Usuario {UserId} solicitando dashboard - últimos {Days} días", userId, days);

                var fromDate = DateTime.UtcNow.AddDays(-days);
                var toDate = DateTime.UtcNow;

                var documentMetricsTask = _documentService.GetDocumentMetricsAsync(userId.Value, fromDate, toDate);
                var velneoMetricsTask = _documentService.GetVelneoIntegrationMetricsAsync(userId.Value, fromDate, toDate);
                var pendingScansTask = _documentService.GetPendingVelneoScansAsync(userId.Value, 10);

                await Task.WhenAll(documentMetricsTask, velneoMetricsTask, pendingScansTask);

                var documentMetrics = await documentMetricsTask;
                var velneoMetrics = await velneoMetricsTask;
                var pendingScans = await pendingScansTask;
                var dashboard = new CompleteDashboardDto
                {
                    TotalScansThisMonth = documentMetrics.TotalScans,
                    BillableScansThisMonth = documentMetrics.BillableScans,
                    SuccessRateThisMonth = documentMetrics.SuccessRate,
                    AverageProcessingTimeMs = documentMetrics.AverageProcessingTimeMs,

                    VelneoMetrics = velneoMetrics,
                    PendingVelneoScans = pendingScans,
                    VelneoSuccessRate = velneoMetrics.VelneoSuccessRate,
                    PendingVelneoCount = velneoMetrics.PendingVelneoCreation,

                    DailyTrends = velneoMetrics.DailyMetrics.Select(d => new DailyTrendDto
                    {
                        Date = d.Date,
                        TotalScans = d.TotalScans,
                        SuccessfulScans = d.VelneoCreated,
                        SuccessRate = d.SuccessRate,
                        AverageProcessingTime = d.AverageProcessingTimeMs
                    }).ToList(),

                    Alerts = GenerateAlerts(documentMetrics, velneoMetrics),

                    QuickStats = new QuickStatsDto
                    {
                        DocumentsToday = GetTodayCount(velneoMetrics.DailyMetrics),
                        VelneoCreatedToday = GetTodayVelneoCount(velneoMetrics.DailyMetrics),
                        PendingReview = pendingScans.Count,
                        AverageAccuracy = documentMetrics.AverageSuccessRate
                    },

                    PeriodInfo = new PeriodInfoDto
                    {
                        Days = days,
                        StartDate = fromDate,
                        EndDate = toDate,
                        GeneratedAt = DateTime.UtcNow
                    }
                };

                _logger.LogInformation("✅ Dashboard generado: {TotalScans} scans, {VelneoSuccessRate:F1}% éxito Velneo",
                    dashboard.TotalScansThisMonth, dashboard.VelneoSuccessRate);

                return Ok(ApiResponse<CompleteDashboardDto>.SuccessResult(
                    dashboard,
                    $"Dashboard generado para los últimos {days} días"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando dashboard");
                return StatusCode(500, ApiResponse<CompleteDashboardDto>.ErrorResult("Error interno del servidor"));
            }
        }

        [HttpGet("pending-velneo")]
        [ProducesResponseType(typeof(ApiResponse<List<DocumentHistoryDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<DocumentHistoryDto>>>> GetPendingVelneoDocuments(
            [FromQuery] int limit = 50)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<List<DocumentHistoryDto>>.ErrorResult("Usuario no autenticado"));
                }

                _logger.LogInformation("🔍 Usuario {UserId} consultando documentos pendientes Velneo", userId);

                var pendingDocs = await _documentService.GetPendingVelneoScansAsync(userId.Value, limit);

                return Ok(ApiResponse<List<DocumentHistoryDto>>.SuccessResult(
                    pendingDocs,
                    $"Se encontraron {pendingDocs.Count} documentos pendientes de envío a Velneo"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo documentos pendientes Velneo");
                return StatusCode(500, ApiResponse<List<DocumentHistoryDto>>.ErrorResult("Error interno del servidor"));
            }
        }

        [HttpGet("velneo-metrics")]
        [ProducesResponseType(typeof(ApiResponse<VelneoIntegrationMetricsDto>), 200)]
        public async Task<ActionResult<ApiResponse<VelneoIntegrationMetricsDto>>> GetVelneoMetrics(
            [FromQuery] int days = 30)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<VelneoIntegrationMetricsDto>.ErrorResult("Usuario no autenticado"));
                }

                var fromDate = DateTime.UtcNow.AddDays(-days);
                var toDate = DateTime.UtcNow;

                _logger.LogInformation("📊 Usuario {UserId} consultando métricas detalladas Velneo", userId);

                var metrics = await _documentService.GetVelneoIntegrationMetricsAsync(userId.Value, fromDate, toDate);

                return Ok(ApiResponse<VelneoIntegrationMetricsDto>.SuccessResult(
                    metrics,
                    $"Métricas de Velneo para los últimos {days} días"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo métricas detalladas Velneo");
                return StatusCode(500, ApiResponse<VelneoIntegrationMetricsDto>.ErrorResult("Error interno del servidor"));
            }
        }

        #region Métodos Auxiliares

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }
            return userId;
        }

        private List<AlertDto> GenerateAlerts(DocumentMetricsDto docMetrics, VelneoIntegrationMetricsDto velneoMetrics)
        {
            var alerts = new List<AlertDto>();

            if (docMetrics.SuccessRate < 70)
            {
                alerts.Add(new AlertDto
                {
                    Type = "warning",
                    Title = "Tasa de éxito baja",
                    Message = $"La tasa de éxito de escaneo es {docMetrics.SuccessRate:F1}% (recomendado: >70%)",
                    Severity = "Medium",
                    ActionRequired = true
                });
            }

            if (velneoMetrics.PendingVelneoCreation > 10)
            {
                alerts.Add(new AlertDto
                {
                    Type = "error",
                    Title = "Documentos pendientes en Velneo",
                    Message = $"{velneoMetrics.PendingVelneoCreation} documentos esperando ser enviados a Velneo",
                    Severity = "High",
                    ActionRequired = true
                });
            }

            if (velneoMetrics.VelneoSuccessRate < 90)
            {
                alerts.Add(new AlertDto
                {
                    Type = "warning",
                    Title = "Tasa de éxito Velneo baja",
                    Message = $"La tasa de éxito de creación en Velneo es {velneoMetrics.VelneoSuccessRate:F1}% (recomendado: >90%)",
                    Severity = "Medium",
                    ActionRequired = false
                });
            }

            if (velneoMetrics.Quality.DocumentsRequiringManualReview > 5)
            {
                alerts.Add(new AlertDto
                {
                    Type = "info",
                    Title = "Documentos requieren revisión",
                    Message = $"{velneoMetrics.Quality.DocumentsRequiringManualReview} documentos requieren revisión manual",
                    Severity = "Low",
                    ActionRequired = false
                });
            }

            return alerts;
        }

        private int GetTodayCount(List<DailyVelneoMetric> dailyMetrics)
        {
            var today = dailyMetrics.FirstOrDefault(d => d.Date.Date == DateTime.Today);
            return today?.TotalScans ?? 0;
        }

        private int GetTodayVelneoCount(List<DailyVelneoMetric> dailyMetrics)
        {
            var today = dailyMetrics.FirstOrDefault(d => d.Date.Date == DateTime.Today);
            return today?.VelneoCreated ?? 0;
        }

        #endregion
    }

    #region DTOs del Dashboard

    public class CompleteDashboardDto
    {
        public int TotalScansThisMonth { get; set; }
        public int BillableScansThisMonth { get; set; }
        public decimal SuccessRateThisMonth { get; set; }
        public int AverageProcessingTimeMs { get; set; }
        public VelneoIntegrationMetricsDto VelneoMetrics { get; set; } = new();
        public List<DocumentHistoryDto> PendingVelneoScans { get; set; } = new();
        public decimal VelneoSuccessRate { get; set; }
        public int PendingVelneoCount { get; set; }
        public List<DailyTrendDto> DailyTrends { get; set; } = new();
        public List<AlertDto> Alerts { get; set; } = new();
        public QuickStatsDto QuickStats { get; set; } = new();
        public PeriodInfoDto PeriodInfo { get; set; } = new();
    }

    public class DailyTrendDto
    {
        public DateTime Date { get; set; }
        public int TotalScans { get; set; }
        public int SuccessfulScans { get; set; }
        public decimal SuccessRate { get; set; }
        public int AverageProcessingTime { get; set; }
    }

    public class AlertDto
    {
        public string Type { get; set; } = ""; 
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Severity { get; set; } = ""; 
        public bool ActionRequired { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class QuickStatsDto
    {
        public int DocumentsToday { get; set; }
        public int VelneoCreatedToday { get; set; }
        public int PendingReview { get; set; }
        public decimal AverageAccuracy { get; set; }
    }

    public class PeriodInfoDto
    {
        public int Days { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    #endregion
}