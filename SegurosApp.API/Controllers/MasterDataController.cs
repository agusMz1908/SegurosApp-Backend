using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Interfaces;
using System.Security.Claims;

namespace SegurosApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MasterDataController : ControllerBase
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly ILogger<MasterDataController> _logger;

        public MasterDataController(
            IVelneoMasterDataService masterDataService,
            ILogger<MasterDataController> logger)
        {
            _masterDataService = masterDataService;
            _logger = logger;
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(CompleteMasterDataResponse), 200)]
        public async Task<ActionResult<CompleteMasterDataResponse>> GetAllMasterData()
        {
            try
            {
                _logger.LogInformation("📋 Usuario {UserId} solicitando master data completo", GetCurrentUserId());

                var masterData = await _masterDataService.GetAllMasterDataAsync();

                _logger.LogInformation("✅ Master data enviado: {Departamentos} dept, {Combustibles} comb, {Corredores} corr",
                    masterData.Departamentos.Count, masterData.Combustibles.Count, masterData.Corredores.Count);

                return Ok(masterData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo master data completo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("suggest-mapping")]
        [ProducesResponseType(typeof(FieldMappingSuggestion), 200)]
        public async Task<ActionResult<FieldMappingSuggestion>> SuggestMapping(
            [FromBody] SuggestMappingRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FieldName) || string.IsNullOrEmpty(request.ScannedValue))
                {
                    return BadRequest(new { message = "FieldName y ScannedValue son requeridos" });
                }

                _logger.LogInformation("🧠 Sugiriendo mapeo para {FieldName}: '{ScannedValue}'",
                    request.FieldName, request.ScannedValue);

                var suggestion = await _masterDataService.SuggestMappingAsync(
                    request.FieldName, request.ScannedValue);

                _logger.LogInformation("💡 Sugerencia: {SuggestedValue} con {Confidence:P1} confianza",
                    suggestion.SuggestedValue, suggestion.Confidence);

                return Ok(suggestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sugiriendo mapeo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("save-mapping")]
        public async Task<ActionResult> SaveMapping([FromBody] SaveMappingRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                _logger.LogInformation("💾 Usuario {UserId} guardando mapeo: {FieldName} '{ScannedValue}' -> '{VelneoValue}'",
                    userId, request.FieldName, request.ScannedValue, request.VelneoValue);

                await _masterDataService.SaveMappingAsync(
                    userId, request.FieldName, request.ScannedValue, request.VelneoValue);

                return Ok(new { message = "Mapeo guardado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error guardando mapeo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("create-poliza")]
        [ProducesResponseType(typeof(CreatePolizaResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<CreatePolizaResponse>> CreatePoliza(
            [FromBody] VelneoPolizaRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new CreatePolizaResponse
                    {
                        Success = false,
                        Message = "Usuario no autenticado"
                    });
                }

                _logger.LogInformation("🚀 Usuario {UserId} creando póliza: Cliente={ClienteId}, Compañía={CompaniaId}, Sección={SeccionId}, Póliza={PolicyNumber}",
                    userId, request.clinro, request.comcod, request.seccod, request.conpol);

                if (request.clinro <= 0 || request.comcod <= 0 || request.seccod <= 0)
                {
                    return BadRequest(new CreatePolizaResponse
                    {
                        Success = false,
                        Message = "Cliente ID, Compañía ID y Sección ID son requeridos"
                    });
                }

                if (string.IsNullOrEmpty(request.conpol))
                {
                    return BadRequest(new CreatePolizaResponse
                    {
                        Success = false,
                        Message = "Número de póliza es requerido"
                    });
                }

                if (request.ingresado == default)
                {
                    request.ingresado = DateTime.UtcNow;
                }

                if (request.last_update == default)
                {
                    request.last_update = DateTime.UtcNow;
                }

                var result = await _masterDataService.CreatePolizaAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("✅ Póliza creada exitosamente: VelneoId={VelneoId}, Número={PolizaNumber}",
                        result.VelneoPolizaId, result.PolizaNumber);

                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("⚠️ Error creando póliza: {Message}", result.Message);
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en endpoint create-poliza");
                return StatusCode(500, new CreatePolizaResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpGet("clientes/search")]
        [ProducesResponseType(typeof(ApiResponse<List<ClienteItem>>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<List<ClienteItem>>>> SearchClientes(
            [FromQuery] string query,
            [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(ApiResponse<List<ClienteItem>>.ErrorResult(
                        "El parámetro 'query' es requerido"));
                }

                if (query.Length < 2)
                {
                    return BadRequest(ApiResponse<List<ClienteItem>>.ErrorResult(
                        "Query debe tener al menos 2 caracteres"));
                }

                if (query.Length > 100)
                {
                    return BadRequest(ApiResponse<List<ClienteItem>>.ErrorResult(
                        "Query no puede tener más de 100 caracteres"));
                }

                if (limit < 1) limit = 20;
                if (limit > 50) limit = 50; 

                var userId = GetCurrentUserId();
                _logger.LogInformation("🔍 Usuario {UserId} buscando clientes: '{Query}' (limit: {Limit})",
                    userId, query, limit);

                var clientes = await _masterDataService.SearchClientesAsync(query, limit);

                var message = clientes.Count switch
                {
                    0 => $"No se encontraron clientes para '{query}'",
                    1 => "Se encontró 1 cliente",
                    _ => $"Se encontraron {clientes.Count} clientes"
                };

                _logger.LogInformation("✅ Búsqueda clientes completada: {Count} resultados para '{Query}'",
                    clientes.Count, query);

                return Ok(ApiResponse<List<ClienteItem>>.SuccessResult(clientes, message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Parámetros inválidos en búsqueda clientes: {Error}", ex.Message);
                return BadRequest(ApiResponse<List<ClienteItem>>.ErrorResult(ex.Message));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conectividad buscando clientes para query '{Query}'", query);
                return StatusCode(503, ApiResponse<List<ClienteItem>>.ErrorResult(
                    "Servicio Velneo temporalmente no disponible"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado buscando clientes para query '{Query}'", query);
                return StatusCode(500, ApiResponse<List<ClienteItem>>.ErrorResult(
                    "Error interno del servidor"));
            }
        }

        [HttpGet("clientes/advanced-search")]
        [ProducesResponseType(typeof(ApiResponse<List<ClienteItem>>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<List<ClienteItem>>>> AdvancedSearchClientes(
    [FromQuery] string? nombre = null,
    [FromQuery] string? direcciones = null,
    [FromQuery] string? clitel = null,
    [FromQuery] string? clicel = null,
    [FromQuery] string? mail = null,
    [FromQuery] string? cliruc = null,
    [FromQuery] string? cliced = null,
    [FromQuery] int limit = 20,
    [FromQuery] bool soloActivos = true)
        {
            try
            {
                var filters = new ClienteSearchFilters
                {
                    Nombre = nombre,
                    Direcciones = direcciones,
                    Clitel = clitel,
                    Clicel = clicel,
                    Mail = mail,
                    Cliruc = cliruc,
                    Cliced = cliced,
                    Limit = limit,
                    SoloActivos = soloActivos
                };

                filters.TrimAndCleanFilters();

                if (!filters.HasAnyFilter())
                {
                    return BadRequest(ApiResponse<List<ClienteItem>>.ErrorResult(
                        "Debe especificar al menos un filtro de búsqueda"));
                }

                if (!TryValidateModel(filters))
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                        .ToList();

                    return BadRequest(ApiResponse<List<ClienteItem>>.ErrorResult(
                        $"Errores de validación: {string.Join("; ", errors)}"));
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("🔍 Usuario {UserId} realizando búsqueda avanzada clientes: {Filters}",
                    userId, filters.ToString());

                var clientes = await _masterDataService.AdvancedSearchClientesAsync(filters);

                var message = GenerateAdvancedSearchResultMessage(clientes.Count, filters);

                _logger.LogInformation("✅ Búsqueda avanzada completada: {Count} resultados con {ActiveFilters} filtros",
                    clientes.Count, filters.GetActiveFiltersCount());

                return Ok(ApiResponse<List<ClienteItem>>.SuccessResult(clientes, message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Parámetros inválidos en búsqueda avanzada: {Error}", ex.Message);
                return BadRequest(ApiResponse<List<ClienteItem>>.ErrorResult(ex.Message));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conectividad en búsqueda avanzada clientes");
                return StatusCode(503, ApiResponse<List<ClienteItem>>.ErrorResult(
                    "Servicio Velneo temporalmente no disponible"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado en búsqueda avanzada clientes");
                return StatusCode(500, ApiResponse<List<ClienteItem>>.ErrorResult(
                    "Error interno del servidor"));
            }
        }

        [HttpGet("clientes/{clienteId}")]
        [ProducesResponseType(typeof(ApiResponse<ClienteItem>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<ClienteItem>>> GetClienteDetalle(int clienteId)
        {
            try
            {
                if (clienteId <= 0)
                {
                    return BadRequest(ApiResponse<ClienteItem>.ErrorResult(
                        "ID de cliente debe ser mayor a 0"));
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("👤 Usuario {UserId} obteniendo detalle cliente {ClienteId}",
                    userId, clienteId);

                var cliente = await _masterDataService.GetClienteDetalleAsync(clienteId);

                if (cliente == null)
                {
                    _logger.LogWarning("⚠️ Cliente {ClienteId} no encontrado en Velneo", clienteId);
                    return NotFound(ApiResponse<ClienteItem>.ErrorResult(
                        $"Cliente con ID {clienteId} no encontrado"));
                }

                var message = cliente.activo
                    ? "Detalle del cliente obtenido exitosamente"
                    : "Cliente encontrado pero está marcado como inactivo";

                _logger.LogInformation("✅ Cliente {ClienteId} obtenido: '{DisplayName}' (Activo: {Activo})",
                    clienteId, cliente.DisplayName, cliente.activo);

                return Ok(ApiResponse<ClienteItem>.SuccessResult(cliente, message));
            }
            catch (FormatException ex)
            {
                _logger.LogWarning("⚠️ ID de cliente inválido: {ClienteId}", clienteId);
                return BadRequest(ApiResponse<ClienteItem>.ErrorResult("ID de cliente inválido"));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Error de conectividad obteniendo cliente {ClienteId}", clienteId);
                return StatusCode(503, ApiResponse<ClienteItem>.ErrorResult(
                    "Servicio Velneo temporalmente no disponible"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado obteniendo detalle cliente {ClienteId}", clienteId);
                return StatusCode(500, ApiResponse<ClienteItem>.ErrorResult(
                    "Error interno del servidor"));
            }
        }

        [HttpGet("departamentos")]
        public async Task<ActionResult<List<DepartamentoItem>>> GetDepartamentos()
        {
            try
            {
                var departamentos = await _masterDataService.GetDepartamentosAsync();
                return Ok(departamentos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo departamentos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("combustibles")]
        public async Task<ActionResult<List<CombustibleItem>>> GetCombustibles()
        {
            try
            {
                var combustibles = await _masterDataService.GetCombustiblesAsync();
                return Ok(combustibles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo combustibles");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("corredores")]
        public async Task<ActionResult<List<CorredorItem>>> GetCorredores()
        {
            try
            {
                var corredores = await _masterDataService.GetCorredoresAsync();
                return Ok(corredores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo corredores");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("categorias")]
        public async Task<ActionResult<List<CategoriaItem>>> GetCategorias()
        {
            try
            {
                var categorias = await _masterDataService.GetCategoriasAsync();
                return Ok(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo categorías");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("destinos")]
        public async Task<ActionResult<List<DestinoItem>>> GetDestinos()
        {
            try
            {
                var destinos = await _masterDataService.GetDestinosAsync();
                return Ok(destinos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo destinos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("calidades")]
        public async Task<ActionResult<List<CalidadItem>>> GetCalidades()
        {
            try
            {
                var calidades = await _masterDataService.GetCalidadesAsync();
                return Ok(calidades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo calidades");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("tarifas")]
        public async Task<ActionResult<List<TarifaItem>>> GetTarifas()
        {
            try
            {
                var tarifas = await _masterDataService.GetTarifasAsync();
                return Ok(tarifas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo tarifas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("companias")]
        [ProducesResponseType(typeof(ApiResponse<List<CompaniaItem>>), 200)]
        public async Task<ActionResult<ApiResponse<List<CompaniaItem>>>> GetCompanias()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("🏢 Usuario {UserId} obteniendo compañías", userId);

                var companias = await _masterDataService.GetCompaniasAsync();

                _logger.LogInformation("✅ Compañías obtenidas: {Count}", companias.Count);

                return Ok(ApiResponse<List<CompaniaItem>>.SuccessResult(
                    companias,
                    $"Se encontraron {companias.Count} compañías"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo compañías");
                return StatusCode(500, ApiResponse<List<CompaniaItem>>.ErrorResult(
                    "Error interno del servidor"));
            }
        }

        [HttpGet("secciones")]
        [ProducesResponseType(typeof(ApiResponse<List<SeccionItem>>), 200)]
        public async Task<ActionResult<ApiResponse<List<SeccionItem>>>> GetSecciones(
            [FromQuery] int? companiaId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("📋 Usuario {UserId} obteniendo secciones (compañía: {CompaniaId})",
                    userId, companiaId?.ToString() ?? "todas");

                var secciones = await _masterDataService.GetSeccionesAsync(companiaId);

                _logger.LogInformation("✅ Secciones obtenidas: {Count}", secciones.Count);

                return Ok(ApiResponse<List<SeccionItem>>.SuccessResult(
                    secciones,
                    $"Se encontraron {secciones.Count} secciones"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo secciones");
                return StatusCode(500, ApiResponse<List<SeccionItem>>.ErrorResult(
                    "Error interno del servidor"));
            }
        }

        [HttpGet("monedas")]
        public async Task<ActionResult<List<MonedaItem>>> GetMonedas()
        {
            try
            {
                var monedas = await _masterDataService.GetMonedasAsync();
                return Ok(monedas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo monedas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("health")]
        public async Task<ActionResult> HealthCheck()
        {
            try
            {
                var combustibles = await _masterDataService.GetCombustiblesAsync();

                return Ok(new
                {
                    status = "healthy",
                    velneoConnected = combustibles.Count > 0,
                    combustiblesCount = combustibles.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Health check falló");
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return 0;
            }
            return userId;
        }

        private string GenerateAdvancedSearchResultMessage(int count, ClienteSearchFilters filters)
        {
            var activeFilters = filters.GetActiveFiltersCount();

            return count switch
            {
                0 => $"No se encontraron clientes con los {activeFilters} filtros especificados",
                1 => $"Se encontró 1 cliente con {activeFilters} filtros aplicados",
                _ => $"Se encontraron {count} clientes con {activeFilters} filtros aplicados"
            };
        }
    }
    public class SuggestMappingRequest
    {
        public string FieldName { get; set; } = string.Empty;
        public string ScannedValue { get; set; } = string.Empty;
    }

    public class SaveMappingRequest
    {
        public string FieldName { get; set; } = string.Empty;
        public string ScannedValue { get; set; } = string.Empty;
        public string VelneoValue { get; set; } = string.Empty;
    }
}