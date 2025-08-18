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
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IAzureDocumentService azureDocumentService,
            IVelneoMasterDataService masterDataService,
            ILogger<DocumentController> logger)
        {
            _azureDocumentService = azureDocumentService;
            _masterDataService = masterDataService;
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

        // ✅ NUEVO: ENDPOINT DE MAPEO EN EL LUGAR CORRECTO
        [HttpPost("{scanId}/map-to-poliza")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> MapToPoliza(int scanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { success = false, message = "Usuario no autenticado" });
                }

                _logger.LogInformation("🔄 Iniciando mapeo de póliza para scan {ScanId} - Usuario: {UserId}",
                    scanId, userId);

                // Obtener datos del escaneo
                var scanData = await _azureDocumentService.GetScanByIdAsync(scanId, userId.Value);
                if (scanData == null)
                {
                    return NotFound(new { success = false, message = "Documento escaneado no encontrado" });
                }

                // ✅ MAPEO BÁSICO POR AHORA - Luego mejoraremos
                var response = new
                {
                    success = true,
                    message = "Datos extraídos del scan listos para mapeo",
                    scanId = scanId,
                    extractedData = scanData.ExtractedData,
                    dataCount = scanData.ExtractedData.Count,
                    fieldsFound = scanData.ExtractedData.Keys.ToList(),
                    // 🎯 CAMPOS PRINCIPALES IDENTIFICADOS
                    mainFields = ExtractMainFields(scanData.ExtractedData)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error mapeando póliza para scan {ScanId}", scanId);
                return StatusCode(500, new { success = false, message = "Error interno del servidor" });
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
                    Limit = Math.Min(limit, 100)
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

        // ===============================
        // MÉTODOS PRIVADOS
        // ===============================

        private object ExtractMainFields(Dictionary<string, object> extractedData)
        {
            return new
            {
                poliza = new
                {
                    numero = CleanPolicyNumber(GetValue(extractedData, "poliza.numero", "datos_poliza")),
                    endoso = CleanEndorsement(GetValue(extractedData, "poliza.endoso")),
                    fechaEmision = CleanDate(GetValue(extractedData, "poliza.fecha_emision")),
                    vigenciaDesde = CleanDate(GetValue(extractedData, "poliza.vigencia.desde")),
                    vigenciaHasta = CleanDate(GetValue(extractedData, "poliza.vigencia.hasta")),
                    tipoMovimiento = CleanMovementType(GetValue(extractedData, "poliza.tipo_movimiento"))
                },
                asegurado = new
                {
                    nombre = CleanCompanyName(GetValue(extractedData, "asegurado.nombre", "datos_asegurado")),
                    documento = CleanDocumentNumber(GetValue(extractedData, "asegurado.documento.numero")),
                    departamento = CleanDepartment(GetValue(extractedData, "asegurado.departamento")),
                    direccion = CleanAddress(GetValue(extractedData, "asegurado.direccion")),
                    localidad = CleanLocality(GetValue(extractedData, "asegurado.localidad"))
                },
                vehiculo = new
                {
                    marca = CleanVehicleBrand(GetValue(extractedData, "vehiculo.marca")),
                    modelo = CleanVehicleModel(GetValue(extractedData, "vehiculo.modelo")),
                    año = CleanYear(GetValue(extractedData, "vehiculo.anio", "vehiculo.año")),
                    motor = CleanMotorNumber(GetValue(extractedData, "vehiculo.motor")),
                    chasis = CleanChassisNumber(GetValue(extractedData, "vehiculo.chasis")),
                    combustible = CleanFuelType(GetValue(extractedData, "vehiculo.combustible")),
                    destino = CleanDestination(GetValue(extractedData, "vehiculo.destino_del_vehiculo")),
                    categoria = CleanCategory(GetValue(extractedData, "vehiculo.tipo_vehiculo"))
                },
                corredor = new
                {
                    nombre = CleanBrokerName(GetValue(extractedData, "corredor.nombre")),
                    numero = CleanBrokerNumber(GetValue(extractedData, "corredor.numero"))
                },
                pago = new
                {
                    medio = CleanPaymentMethod(GetValue(extractedData, "pago.medio")),
                    cuotas = CleanInstallmentCount(GetValue(extractedData, "pago.modo_facturacion")),
                    primaComercial = CleanAmount(GetValue(extractedData, "poliza.prima_comercial", "financiero.prima_comercial")),
                    premioTotal = CleanAmount(GetValue(extractedData, "financiero.premio_total"))
                }
            };
        }

        private string CleanPolicyNumber(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            // Buscar patrón de número de póliza (7-9 dígitos)
            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"\b(\d{7,9})\b");
            return match.Success ? match.Groups[1].Value : "";
        }

        /// <summary>
        /// Limpia endoso - extrae solo el número
        /// </summary>
        private string CleanEndorsement(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "0";

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"\b(\d+)\b");
            return match.Success ? match.Groups[1].Value : "0";
        }

        /// <summary>
        /// Limpia fechas - extrae en formato dd/MM/yyyy
        /// </summary>
        private string CleanDate(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            // Buscar patrón de fecha
            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"(\d{1,2})/(\d{1,2})/(\d{4})");
            return match.Success ? match.Value : "";
        }

        /// <summary>
        /// Limpia nombre de empresa - remueve etiquetas
        /// </summary>
        private string CleanCompanyName(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Asegurado:", "")
                .Replace("\n", " ")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia número de documento - solo números
        /// </summary>
        private string CleanDocumentNumber(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return System.Text.RegularExpressions.Regex.Replace(rawValue, @"[^\d]", "");
        }

        /// <summary>
        /// Limpia departamento - remueve etiquetas
        /// </summary>
        private string CleanDepartment(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Depto:", "")
                .Replace("Departamento:", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia dirección
        /// </summary>
        private string CleanAddress(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Dirección:", "")
                .Replace("\n", " ")
                .Replace("\r", "")
                .Trim();
        }

        /// <summary>
        /// Limpia localidad
        /// </summary>
        private string CleanLocality(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Localidad:", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
        }

        /// <summary>
        /// Limpia marca de vehículo
        /// </summary>
        private string CleanVehicleBrand(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("MARCA", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia modelo de vehículo
        /// </summary>
        private string CleanVehicleModel(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("MODELO", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
        }

        /// <summary>
        /// Limpia año - extrae solo el año
        /// </summary>
        private int CleanYear(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return 0;

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"\b(20\d{2}|19\d{2})\b");
            return match.Success && int.TryParse(match.Groups[1].Value, out var year) ? year : 0;
        }

        /// <summary>
        /// Limpia número de motor
        /// </summary>
        private string CleanMotorNumber(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("MOTOR", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia número de chasis
        /// </summary>
        private string CleanChassisNumber(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("CHASIS", "")
                .Replace("CHASSIS", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia tipo de combustible
        /// </summary>
        private string CleanFuelType(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("COMBUSTIBLE", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia destino del vehículo
        /// </summary>
        private string CleanDestination(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("DESTINO DEL VEHÍCULO.", "")
                .Replace("DESTINO DEL VEHICULO", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia categoría del vehículo
        /// </summary>
        private string CleanCategory(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("TIPO DE VEHÍCULO.", "")
                .Replace("TIPO DE VEHICULO", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
        }

        /// <summary>
        /// Limpia nombre del corredor
        /// </summary>
        private string CleanBrokerName(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Nombre:", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia número del corredor
        /// </summary>
        private int CleanBrokerNumber(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return 0;

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"(\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var number) ? number : 0;
        }

        /// <summary>
        /// Limpia medio de pago
        /// </summary>
        private string CleanPaymentMethod(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Medio de Pago:", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Extrae cantidad de cuotas
        /// </summary>
        private int CleanInstallmentCount(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return 1;

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"(\d{1,2})\s*cuotas?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : 1;
        }

        /// <summary>
        /// Limpia tipo de movimiento
        /// </summary>
        private string CleanMovementType(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Tipo de movimiento:", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Limpia montos - extrae solo el número
        /// </summary>
        private decimal CleanAmount(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return 0;

            // Buscar patrón de dinero: $ 63.812,36
            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"\$\s*([0-9]{1,3}(?:\.[0-9]{3})*(?:,[0-9]{2})?)");
            if (match.Success)
            {
                var amountStr = match.Groups[1].Value
                    .Replace(".", "")  // Quitar separadores de miles
                    .Replace(",", "."); // Convertir coma decimal a punto

                return decimal.TryParse(amountStr, System.Globalization.NumberStyles.Currency,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount) ? amount : 0;
            }

            return 0;
        }

        private string GetValue(Dictionary<string, object> data, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (data.TryGetValue(key, out var value) && value != null)
                {
                    var text = value.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
            }
            return "";
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