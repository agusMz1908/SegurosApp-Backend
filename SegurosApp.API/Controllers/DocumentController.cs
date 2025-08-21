using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
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
        private readonly PolizaMapperService _polizaMapperService;
        private readonly AppDbContext _context;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IAzureDocumentService azureDocumentService,
            IVelneoMasterDataService masterDataService,
            PolizaMapperService polizaMapperService,
            AppDbContext context,
            ILogger<DocumentController> logger)
        {
            _azureDocumentService = azureDocumentService;
            _masterDataService = masterDataService;
            _polizaMapperService = polizaMapperService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("upload-with-context")]
        [ProducesResponseType(typeof(DocumentScanWithContextResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<DocumentScanWithContextResponse>> UploadDocumentWithContext(
            IFormFile file,
            [FromForm] int clienteId,
            [FromForm] int companiaId,
            [FromForm] int seccionId,
            [FromForm] string? notes = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new DocumentScanWithContextResponse
                    {
                        Success = false,
                        ErrorMessage = "Archivo requerido"
                    });
                }

                if (clienteId <= 0 || companiaId <= 0 || seccionId <= 0)
                {
                    return BadRequest(new DocumentScanWithContextResponse
                    {
                        Success = false,
                        ErrorMessage = "Debe seleccionar Cliente, Compañía y Sección antes de escanear"
                    });
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new DocumentScanWithContextResponse
                    {
                        Success = false,
                        ErrorMessage = "Usuario no autenticado"
                    });
                }

                _logger.LogInformation("📄 Upload con contexto iniciado: {FileName} - Cliente:{ClienteId}, Compañía:{CompaniaId}, Sección:{SeccionId}",
                    file.FileName, clienteId, companiaId, seccionId);

                var validationResult = await ValidatePreSelectionAsync(clienteId, companiaId, seccionId);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new DocumentScanWithContextResponse
                    {
                        Success = false,
                        ErrorMessage = validationResult.ErrorMessage,
                        ValidationErrors = validationResult.Errors
                    });
                }

                var scanResult = await _azureDocumentService.ProcessDocumentAsync(file, userId.Value);
                if (!scanResult.Success)
                {
                    return BadRequest(new DocumentScanWithContextResponse
                    {
                        Success = false,
                        ErrorMessage = scanResult.ErrorMessage,
                        ScanResult = scanResult
                    });
                }

                await SaveContextToScanAsync(scanResult.ScanId, clienteId, companiaId, seccionId, notes);

                var polizaMapping = await _polizaMapperService.MapToPolizaWithContextAsync(
                    scanResult.ExtractedData,
                    new PreSelectionContext
                    {
                        ClienteId = clienteId,
                        CompaniaId = companiaId,
                        SeccionId = seccionId,
                        ScanId = scanResult.ScanId,
                        UserId = userId.Value,
                        Notes = notes
                    }
                );

                var response = new DocumentScanWithContextResponse
                {
                    Success = true,
                    ScanResult = scanResult,
                    PreSelection = validationResult.ValidatedData!,
                    PolizaMapping = polizaMapping,
                    IsReadyForVelneo = polizaMapping.IsComplete,
                    Message = polizaMapping.IsComplete
                        ? "Documento procesado y mapeado exitosamente - Listo para enviar a Velneo"
                        : "Documento procesado - Requiere revisión manual antes de enviar"
                };

                _logger.LogInformation("✅ Upload con contexto completado: {ScanId} - Listo para Velneo: {IsReady}",
                    scanResult.ScanId, response.IsReadyForVelneo);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en upload con contexto: {FileName}", file?.FileName ?? "unknown");
                return StatusCode(500, new DocumentScanWithContextResponse
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor",
                    ScanResult = new DocumentScanResponse { FileName = file?.FileName ?? "unknown" }
                });
            }
        }

        private async Task<PreSelectionValidationResult> ValidatePreSelectionAsync(
            int clienteId, int companiaId, int seccionId)
        {
            try
            {
                _logger.LogInformation("🔍 Validando pre-selección: Cliente={ClienteId}, Compañía={CompaniaId}, Sección={SeccionId}",
                    clienteId, companiaId, seccionId);

                var validationErrors = new List<string>();
                ClienteItem? cliente = null;
                CompaniaItem? compania = null;
                SeccionItem? seccion = null;

                try
                {
                    cliente = await _masterDataService.GetClienteDetalleAsync(clienteId);
                    if (cliente == null)
                    {
                        validationErrors.Add($"Cliente con ID {clienteId} no encontrado en Velneo");
                        _logger.LogWarning("❌ Cliente {ClienteId} no encontrado", clienteId);
                    }
                    else if (!cliente.activo)
                    {
                        validationErrors.Add($"Cliente '{cliente.DisplayName}' está marcado como inactivo");
                        _logger.LogWarning("⚠️ Cliente {ClienteId} está inactivo", clienteId);
                    }
                    else
                    {
                        _logger.LogInformation("✅ Cliente validado: {ClienteId} - {DisplayName}",
                            clienteId, cliente.DisplayName);
                    }
                }
                catch (HttpRequestException ex)
                {
                    validationErrors.Add("Error de conectividad validando cliente - Servicio Velneo no disponible");
                    _logger.LogError(ex, "❌ Error de conectividad validando cliente {ClienteId}", clienteId);
                }
                catch (Exception ex)
                {
                    validationErrors.Add($"Error inesperado validando cliente: {ex.Message}");
                    _logger.LogError(ex, "❌ Error inesperado validando cliente {ClienteId}", clienteId);
                }

                try
                {
                    var companias = await _masterDataService.GetCompaniasAsync();
                    compania = companias.FirstOrDefault(c => c.id == companiaId);

                    if (compania == null)
                    {
                        validationErrors.Add($"Compañía con ID {companiaId} no encontrada en Velneo");
                        _logger.LogWarning("❌ Compañía {CompaniaId} no encontrada", companiaId);
                    }
                    else if (!compania.IsActive)
                    {
                        validationErrors.Add($"Compañía '{compania.DisplayName}' está marcada como inactiva");
                        _logger.LogWarning("⚠️ Compañía {CompaniaId} está inactiva", companiaId);
                    }
                    else
                    {
                        _logger.LogInformation("✅ Compañía validada: {CompaniaId} - {DisplayName}",
                            companiaId, compania.DisplayName);
                    }
                }
                catch (HttpRequestException ex)
                {
                    validationErrors.Add("Error de conectividad validando compañía - Servicio Velneo no disponible");
                    _logger.LogError(ex, "❌ Error de conectividad validando compañía {CompaniaId}", companiaId);
                }
                catch (Exception ex)
                {
                    validationErrors.Add($"Error inesperado validando compañía: {ex.Message}");
                    _logger.LogError(ex, "❌ Error inesperado validando compañía {CompaniaId}", companiaId);
                }

                try
                {
                    var secciones = await _masterDataService.GetSeccionesAsync(); 
                    seccion = secciones.FirstOrDefault(s => s.id == seccionId);

                    if (seccion == null)
                    {
                        validationErrors.Add($"Sección con ID {seccionId} no encontrada en Velneo");
                        _logger.LogWarning("❌ Sección {SeccionId} no encontrada", seccionId);

                        if (secciones.Any())
                        {
                            var seccionesDisponibles = string.Join(", ", secciones.Take(5).Select(s => $"{s.id}:{s.DisplayName}"));
                            validationErrors.Add($"Secciones disponibles: {seccionesDisponibles}");
                        }
                        else
                        {
                            validationErrors.Add("No hay secciones disponibles en el sistema");
                        }
                    }
                    else if (!seccion.IsActive)
                    {
                        validationErrors.Add($"Sección '{seccion.DisplayName}' está marcada como inactiva");
                        _logger.LogWarning("⚠️ Sección {SeccionId} está inactiva", seccionId);
                    }
                    else
                    {
                        _logger.LogInformation("✅ Sección validada: {SeccionId} - {DisplayName}",
                            seccionId, seccion.DisplayName);
                    }
                }
                catch (HttpRequestException ex)
                {
                    validationErrors.Add("Error de conectividad validando sección - Servicio Velneo no disponible");
                    _logger.LogError(ex, "❌ Error de conectividad validando sección {SeccionId}", seccionId);
                }
                catch (Exception ex)
                {
                    validationErrors.Add($"Error inesperado validando sección: {ex.Message}");
                    _logger.LogError(ex, "❌ Error inesperado validando sección {SeccionId}", seccionId);
                }

                if (cliente != null && compania != null && seccion != null)
                {
                    _logger.LogDebug("🔍 Validaciones lógicas: Cliente={ClienteId}, Compañía={CompaniaId}, Sección={SeccionId} - Todos válidos e independientes",
                        clienteId, companiaId, seccionId);
                }

                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning("❌ Validación falló: {ErrorCount} errores - {Errors}",
                        validationErrors.Count, errorMessage);

                    return new PreSelectionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Errores de validación: {errorMessage}",
                        Errors = validationErrors,
                        ValidatedData = null
                    };
                }

                var validatedData = new ValidatedPreSelection
                {
                    Cliente = cliente!,
                    Compania = compania!,
                    Seccion = seccion!
                };

                _logger.LogInformation("✅ Pre-selección validada exitosamente: Cliente='{ClienteName}', Compañía='{CompaniaName}', Sección='{SeccionName}'",
                    validatedData.ClienteDisplayName, validatedData.CompaniaDisplayName, validatedData.SeccionDisplayName);

                return new PreSelectionValidationResult
                {
                    IsValid = true,
                    ErrorMessage = string.Empty,
                    Errors = new List<string>(),
                    ValidatedData = validatedData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error crítico en validación de pre-selección");

                return new PreSelectionValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Error crítico en validación: {ex.Message}",
                    Errors = new List<string> { $"Error crítico: {ex.Message}" },
                    ValidatedData = null
                };
            }
        }

        [HttpPost("{scanId}/create-in-velneo")]
        [ProducesResponseType(typeof(CreatePolizaVelneoResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CreatePolizaVelneoResponse>> CreatePolizaInVelneo(
            int scanId,
            [FromBody] CreatePolizaVelneoRequest? overrides = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new CreatePolizaVelneoResponse
                    {
                        Success = false,
                        ErrorMessage = "Usuario no autenticado"
                    });
                }

                _logger.LogInformation("🚀 Creando póliza en Velneo para scan {ScanId}", scanId);

                var scanData = await _azureDocumentService.GetScanByIdAsync(scanId, userId.Value);
                if (scanData == null)
                {
                    return NotFound(new CreatePolizaVelneoResponse
                    {
                        Success = false,
                        ErrorMessage = "Documento escaneado no encontrado"
                    });
                }

                var velneoRequest = await _polizaMapperService.CreateVelneoRequestFromScanAsync(scanId, userId.Value, overrides);
                var velneoResult = await _masterDataService.CreatePolizaAsync(velneoRequest);

                if (velneoResult.Success)
                {
                    await _azureDocumentService.UpdateScanWithVelneoInfoAsync(
                        scanId,
                        velneoResult.VelneoPolizaId?.ToString(),
                        true);

                    _logger.LogInformation("✅ Póliza creada exitosamente en Velneo: ScanId={ScanId}, VelneoId={VelneoId}",
                        scanId, velneoResult.VelneoPolizaId);
                }

                return Ok(new CreatePolizaVelneoResponse
                {
                    Success = velneoResult.Success,
                    Message = velneoResult.Message,
                    ScanId = scanId,
                    VelneoPolizaId = velneoResult.VelneoPolizaId,
                    PolizaNumber = velneoResult.PolizaNumber,
                    CreatedAt = velneoResult.CreatedAt,
                    Warnings = velneoResult.Warnings,
                    Validation = velneoResult.Validation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando póliza en Velneo para scan {ScanId}", scanId);
                return StatusCode(500, new CreatePolizaVelneoResponse
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
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

                var scanData = await _azureDocumentService.GetScanByIdAsync(scanId, userId.Value);
                if (scanData == null)
                {
                    return NotFound(new { success = false, message = "Documento escaneado no encontrado" });
                }

                var response = new
                {
                    success = true,
                    message = "Datos extraídos del scan listos para mapeo",
                    scanId = scanId,
                    extractedData = scanData.ExtractedData,
                    dataCount = scanData.ExtractedData.Count,
                    fieldsFound = scanData.ExtractedData.Keys.ToList(),
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
            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"\b(\d{7,9})\b");
            return match.Success ? match.Groups[1].Value : "";
        }

        private string CleanEndorsement(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "0";

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"\b(\d+)\b");
            return match.Success ? match.Groups[1].Value : "0";
        }

        private string CleanDate(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"(\d{1,2})/(\d{1,2})/(\d{4})");
            return match.Success ? match.Value : "";
        }

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

        private string CleanDocumentNumber(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return System.Text.RegularExpressions.Regex.Replace(rawValue, @"[^\d]", "");
        }
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
        private string CleanAddress(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Dirección:", "")
                .Replace("\n", " ")
                .Replace("\r", "")
                .Trim();
        }
        private string CleanLocality(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("Localidad:", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
        }
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
        private string CleanVehicleModel(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return "";

            return rawValue
                .Replace("MODELO", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();
        }
        private int CleanYear(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return 0;

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"\b(20\d{2}|19\d{2})\b");
            return match.Success && int.TryParse(match.Groups[1].Value, out var year) ? year : 0;
        }

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

        private int CleanBrokerNumber(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return 0;

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"(\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var number) ? number : 0;
        }

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

        private int CleanInstallmentCount(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return 1;

            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"(\d{1,2})\s*cuotas?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : 1;
        }

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
        private decimal CleanAmount(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue)) return 0;
            var match = System.Text.RegularExpressions.Regex.Match(rawValue, @"\$\s*([0-9]{1,3}(?:\.[0-9]{3})*(?:,[0-9]{2})?)");
            if (match.Success)
            {
                var amountStr = match.Groups[1].Value
                    .Replace(".", "")  
                    .Replace(",", "."); 

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

        private async Task SaveContextToScanAsync(int scanId, int clienteId, int companiaId, int seccionId, string? notes)
        {
            try
            {
                var scan = await _context.DocumentScans.FindAsync(scanId);
                if (scan == null)
                {
                    _logger.LogWarning("⚠️ Scan {ScanId} no encontrado para guardar contexto", scanId);
                    return;
                }

                scan.ClienteId = clienteId;
                scan.CompaniaId = companiaId;
                scan.SeccionId = seccionId;
                scan.PreSelectionNotes = notes;
                scan.PreSelectionSavedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Contexto guardado en scan {ScanId}: Cliente={ClienteId}, Compañía={CompaniaId}, Sección={SeccionId}",
                    scanId, clienteId, companiaId, seccionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error guardando contexto en scan {ScanId}", scanId);
                throw;
            }
        }
    }
}