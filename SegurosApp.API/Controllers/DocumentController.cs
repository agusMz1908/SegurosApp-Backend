using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.DTOs;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Services;
using System.Security.Claims;

namespace SegurosApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IAzureDocumentService _azureDocumentService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IAzureDocumentService azureDocumentService,
            ILogger<DocumentController> logger)
        {
            _azureDocumentService = azureDocumentService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [ProducesResponseType(typeof(DocumentScanResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<DocumentScanResponse>> UploadDocument(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new DocumentScanResponse
                    {
                        Success = false,
                        ErrorMessage = "Archivo requerido"
                    });
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new DocumentScanResponse
                    {
                        Success = false,
                        ErrorMessage = "Usuario no autenticado"
                    });
                }

                _logger.LogInformation("📄 Upload de documento iniciado: {FileName} por usuario: {UserId}",
                    file.FileName, userId);

                var result = await _azureDocumentService.ProcessDocumentAsync(file, userId.Value);

                if (!result.Success)
                {
                    _logger.LogWarning("⚠️ Error procesando documento: {FileName} - {Error}",
                        file.FileName, result.ErrorMessage);
                    return BadRequest(result);
                }

                if (result.IsDuplicate)
                {
                    _logger.LogInformation("🔄 Documento duplicado detectado: {FileName}", file.FileName);
                }
                else
                {
                    _logger.LogInformation("✅ Documento procesado exitosamente: {ScanId} - {FileName}",
                        result.ScanId, file.FileName);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en upload de documento: {FileName}", file?.FileName ?? "unknown");
                return StatusCode(500, new DocumentScanResponse
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor",
                    FileName = file?.FileName ?? "unknown"
                });
            }
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<List<DocumentHistoryDto>>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponse<List<DocumentHistoryDto>>>> GetDocumentHistory(
            [FromQuery] string? fileName = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool? isBillable = null,
            [FromQuery] bool? velneoCreated = null,
            [FromQuery] decimal? minSuccessRate = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<List<DocumentHistoryDto>>.ErrorResult("Usuario no autenticado"));
                }

                var filters = new DocumentSearchFilters
                {
                    FileName = fileName,
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    IsBillable = isBillable,
                    VelneoCreated = velneoCreated,
                    MinSuccessRate = minSuccessRate,
                    Page = page,
                    Limit = Math.Min(limit, 100) // Máximo 100 por página
                };

                _logger.LogInformation("📋 Consultando historial de documentos - Usuario: {UserId}, Página: {Page}",
                    userId, page);

                var history = await _azureDocumentService.GetScanHistoryAsync(userId.Value, filters);

                return Ok(ApiResponse<List<DocumentHistoryDto>>.SuccessResult(
                    history,
                    $"Se encontraron {history.Count} documentos"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo historial de documentos");
                return StatusCode(500, ApiResponse<List<DocumentHistoryDto>>.ErrorResult("Error interno del servidor"));
            }
        }

        [HttpGet("{scanId}")]
        [ProducesResponseType(typeof(DocumentHistoryDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<DocumentHistoryDto>> GetDocumentDetail(int scanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }

                _logger.LogInformation("🔍 Consultando detalle de documento: {ScanId} - Usuario: {UserId}",
                    scanId, userId);

                var document = await _azureDocumentService.GetScanByIdAsync(scanId, userId.Value);

                if (document == null)
                {
                    _logger.LogWarning("⚠️ Documento no encontrado: {ScanId}", scanId);
                    return NotFound(new { message = "Documento no encontrado" });
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo detalle de documento: {ScanId}", scanId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("metrics")]
        [ProducesResponseType(typeof(DocumentMetricsDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<DocumentMetricsDto>> GetDocumentMetrics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }

                // Por defecto, últimos 30 días
                fromDate ??= DateTime.UtcNow.AddDays(-30);
                toDate ??= DateTime.UtcNow;

                _logger.LogInformation("📊 Consultando métricas - Usuario: {UserId}, Desde: {From}, Hasta: {To}",
                    userId, fromDate, toDate);

                var metrics = await _azureDocumentService.GetDocumentMetricsAsync(userId.Value, fromDate, toDate);

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo métricas de documentos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("{scanId}/reprocess")]
        [ProducesResponseType(typeof(DocumentScanResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<DocumentScanResponse>> ReprocessDocument(
            int scanId,
            [FromBody] ReprocessDocumentRequest? request = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new DocumentScanResponse
                    {
                        Success = false,
                        ErrorMessage = "Usuario no autenticado"
                    });
                }

                var forceReprocess = request?.ForceReprocess ?? false;

                _logger.LogInformation("🔄 Reprocesando documento: {ScanId} - Usuario: {UserId}, Forzado: {Force}",
                    scanId, userId, forceReprocess);

                var result = await _azureDocumentService.ReprocessDocumentAsync(scanId, userId.Value, forceReprocess);

                if (!result.Success)
                {
                    _logger.LogWarning("⚠️ Error reprocesando documento: {ScanId} - {Error}",
                        scanId, result.ErrorMessage);

                    if (result.ErrorMessage?.Contains("no encontrado") == true)
                        return NotFound(result);

                    return BadRequest(result);
                }

                _logger.LogInformation("✅ Documento reprocesado exitosamente: {ScanId}", scanId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error reprocesando documento: {ScanId}", scanId);
                return StatusCode(500, new DocumentScanResponse
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        [HttpGet("config")]
        [ProducesResponseType(typeof(object), 200)]
        public ActionResult GetUploadConfig()
        {
            try
            {
                var config = new
                {
                    maxFileSizeMB = 10,
                    allowedExtensions = new[] { ".pdf" },
                    allowedMimeTypes = new[] { "application/pdf" },
                    processingTimeoutSeconds = 120,
                    supportedLanguages = new[] { "es", "en" },
                    azureModelInfo = new
                    {
                        modelId = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["AzureDocumentIntelligence:ModelId"],
                        isCustomModel = true,
                        description = "Modelo personalizado para pólizas de seguros"
                    }
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo configuración");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }


        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public ActionResult GetHealthStatus()
        {
            try
            {
                var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var endpoint = config["AzureDocumentIntelligence:Endpoint"];
                var hasApiKey = !string.IsNullOrEmpty(config["AzureDocumentIntelligence:ApiKey"]);

                var health = new
                {
                    status = "healthy",
                    azureEndpoint = endpoint,
                    hasApiKey = hasApiKey,
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0"
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en health check");
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }
            return userId;
        }
    }

}