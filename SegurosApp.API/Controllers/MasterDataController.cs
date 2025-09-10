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
                _logger.LogInformation("Usuario {UserId} solicitando master data completo", GetCurrentUserId());

                var masterData = await _masterDataService.GetAllMasterDataAsync();

                _logger.LogInformation("Master data enviado: {Departamentos} dept, {Combustibles} comb, {Corredores} corr",
                    masterData.Departamentos.Count, masterData.Combustibles.Count, masterData.Corredores.Count);

                return Ok(masterData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo master data completo");
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

                _logger.LogInformation("Sugiriendo mapeo para {FieldName}: '{ScannedValue}'",
                    request.FieldName, request.ScannedValue);

                var suggestion = await _masterDataService.SuggestMappingAsync(
                    request.FieldName, request.ScannedValue);

                _logger.LogInformation("Sugerencia: {SuggestedValue} con {Confidence:P1} confianza",
                    suggestion.SuggestedValue, suggestion.Confidence);

                return Ok(suggestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sugiriendo mapeo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("save-mapping")]
        public async Task<ActionResult> SaveMapping([FromBody] SaveMappingRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                _logger.LogInformation("Usuario {UserId} guardando mapeo: {FieldName} '{ScannedValue}' -> '{VelneoValue}'",
                    userId, request.FieldName, request.ScannedValue, request.VelneoValue);

                await _masterDataService.SaveMappingAsync(
                    userId, request.FieldName, request.ScannedValue, request.VelneoValue);

                return Ok(new { message = "Mapeo guardado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando mapeo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("polizas")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> GetPolizas(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? numeroPoliza = null,
            [FromQuery] int? clienteId = null,
            [FromQuery] int? companiaId = null,
            [FromQuery] int? seccionId = null,
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] bool soloActivos = true)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} obteniendo pólizas - Página: {Page}, Tamaño: {PageSize}",
                    userId, page, pageSize);

                PolizaSearchFilters? filters = null;
                if (!string.IsNullOrEmpty(numeroPoliza) || clienteId.HasValue ||
                    companiaId.HasValue || seccionId.HasValue ||
                    !string.IsNullOrEmpty(estado) || fechaDesde.HasValue || fechaHasta.HasValue)
                {
                    filters = new PolizaSearchFilters
                    {
                        NumeroPoliza = numeroPoliza,
                        ClienteId = clienteId,
                        CompaniaId = companiaId,
                        SeccionId = seccionId,
                        Estado = estado,
                        FechaDesde = fechaDesde,
                        FechaHasta = fechaHasta,
                        SoloActivos = soloActivos
                    };
                    filters.TrimAndCleanFilters();

                    if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde > fechaHasta)
                    {
                        return BadRequest(new { message = "La fecha desde no puede ser mayor que la fecha hasta" });
                    }
                }

                var result = await _masterDataService.GetPolizasPaginatedAsync(page, pageSize, filters);

                var response = new
                {
                    data = result.Items,
                    pagination = new
                    {
                        page = result.Page,
                        pageSize = result.PageSize,
                        count = result.Count,
                        totalCount = result.TotalCount,
                        totalPages = result.TotalPages,
                        hasNextPage = result.HasNextPage,
                        hasPreviousPage = result.HasPreviousPage,
                        startIndex = result.StartIndex,
                        endIndex = result.EndIndex
                    },
                    filters = new
                    {
                        applied = filters != null,
                        numeroPoliza,
                        clienteId,
                        companiaId,
                        seccionId,
                        estado,
                        fechaDesde = fechaDesde?.ToString("yyyy-MM-dd"),
                        fechaHasta = fechaHasta?.ToString("yyyy-MM-dd"),
                        soloActivos,
                        activeFiltersCount = filters?.GetActiveFiltersCount() ?? 0
                    },
                    metadata = new
                    {
                        timestamp = DateTime.UtcNow,
                        dataType = "polizas"
                    }
                };

                _logger.LogInformation("Pólizas obtenidas: {Count}/{TotalCount} en página {Page}",
                    result.Count, result.TotalCount, page);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Parámetros inválidos en pólizas: {Error}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conectividad obteniendo pólizas");
                return StatusCode(503, new { message = "Servicio Velneo temporalmente no disponible" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado obteniendo pólizas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("polizas/search")]
        [ProducesResponseType(typeof(ApiResponse<List<ContratoItem>>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<List<ContratoItem>>>> SearchPolizasQuick(
            [FromQuery] string? numeroPoliza,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(numeroPoliza))
                {
                    return BadRequest(ApiResponse<List<ContratoItem>>.ErrorResult(
                        "El parámetro 'numeroPoliza' es requerido"));
                }

                if (numeroPoliza.Length < 2)
                {
                    return BadRequest(ApiResponse<List<ContratoItem>>.ErrorResult(
                        "El número de póliza debe tener al menos 2 caracteres"));
                }

                if (numeroPoliza.Length > 50)
                {
                    return BadRequest(ApiResponse<List<ContratoItem>>.ErrorResult(
                        "El número de póliza no puede tener más de 50 caracteres"));
                }

                if (limit < 1) limit = 10;
                if (limit > 50) limit = 50;

                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} búsqueda rápida pólizas: '{NumeroPoliza}' (limit: {Limit})",
                    userId, numeroPoliza, limit);

                var polizas = await _masterDataService.SearchPolizasQuickAsync(numeroPoliza, limit);

                var message = polizas.Count switch
                {
                    0 => $"No se encontraron pólizas para '{numeroPoliza}'",
                    1 => "Se encontró 1 póliza",
                    _ => $"Se encontraron {polizas.Count} pólizas"
                };

                _logger.LogInformation("Búsqueda rápida pólizas completada: {Count} resultados para '{NumeroPoliza}'",
                    polizas.Count, numeroPoliza);

                return Ok(ApiResponse<List<ContratoItem>>.SuccessResult(polizas, message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Parámetros inválidos en búsqueda rápida pólizas: {Error}", ex.Message);
                return BadRequest(ApiResponse<List<ContratoItem>>.ErrorResult(ex.Message));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conectividad en búsqueda rápida pólizas para '{NumeroPoliza}'", numeroPoliza);
                return StatusCode(503, ApiResponse<List<ContratoItem>>.ErrorResult(
                    "Servicio Velneo temporalmente no disponible"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en búsqueda rápida pólizas para '{NumeroPoliza}'", numeroPoliza);
                return StatusCode(500, ApiResponse<List<ContratoItem>>.ErrorResult(
                    "Error interno del servidor"));
            }
        }

        [HttpGet("polizas/{polizaId}")]
        [ProducesResponseType(typeof(ApiResponse<ContratoItem>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<ContratoItem>>> GetPolizaDetalle(int polizaId)
        {
            try
            {
                if (polizaId <= 0)
                {
                    return BadRequest(ApiResponse<ContratoItem>.ErrorResult(
                        "ID de póliza debe ser mayor a 0"));
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} obteniendo detalle póliza {PolizaId}",
                    userId, polizaId);

                var poliza = await _masterDataService.GetPolizaDetalleAsync(polizaId);

                if (poliza == null)
                {
                    _logger.LogWarning("Póliza {PolizaId} no encontrada en Velneo", polizaId);
                    return NotFound(ApiResponse<ContratoItem>.ErrorResult(
                        $"Póliza con ID {polizaId} no encontrada"));
                }

                var message = "Detalle de la póliza obtenido exitosamente";

                _logger.LogInformation("Póliza {PolizaId} obtenida: '{NumeroPoliza}' - Cliente: {ClienteId}",
                    polizaId, poliza.conpol, poliza.clinro);

                return Ok(ApiResponse<ContratoItem>.SuccessResult(poliza, message));
            }
            catch (FormatException ex)
            {
                _logger.LogWarning("ID de póliza inválido: {PolizaId}", polizaId);
                return BadRequest(ApiResponse<ContratoItem>.ErrorResult("ID de póliza inválido"));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conectividad obteniendo póliza {PolizaId}", polizaId);
                return StatusCode(503, ApiResponse<ContratoItem>.ErrorResult(
                    "Servicio Velneo temporalmente no disponible"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado obteniendo detalle póliza {PolizaId}", polizaId);
                return StatusCode(500, ApiResponse<ContratoItem>.ErrorResult(
                    "Error interno del servidor"));
            }
        }

        [HttpGet("clientes/{clienteId}/polizas")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> GetPolizasByCliente(
            int clienteId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool soloActivos = true)
        {
            try
            {
                if (clienteId <= 0)
                {
                    return BadRequest(new { message = "ID de cliente debe ser mayor a 0" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} obteniendo pólizas del cliente {ClienteId}",
                    userId, clienteId);

                var filters = new PolizaSearchFilters
                {
                    ClienteId = clienteId,
                    SoloActivos = soloActivos
                };

                var result = await _masterDataService.GetPolizasPaginatedAsync(page, pageSize, filters);

                var response = new
                {
                    data = result.Items,
                    pagination = new
                    {
                        page = result.Page,
                        pageSize = result.PageSize,
                        count = result.Count,
                        totalCount = result.TotalCount,
                        totalPages = result.TotalPages,
                        hasNextPage = result.HasNextPage,
                        hasPreviousPage = result.HasPreviousPage,
                        startIndex = result.StartIndex,
                        endIndex = result.EndIndex
                    },
                    cliente = new
                    {
                        id = clienteId,
                        soloActivos
                    },
                    metadata = new
                    {
                        timestamp = DateTime.UtcNow,
                        dataType = "polizas_by_cliente"
                    }
                };

                _logger.LogInformation("Pólizas del cliente {ClienteId} obtenidas: {Count}/{TotalCount}",
                    clienteId, result.Count, result.TotalCount);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo pólizas del cliente {ClienteId}", clienteId);
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

        [HttpGet("clientes")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> GetClientes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? nombre = null,
            [FromQuery] string? cliced = null,
            [FromQuery] string? clicel = null,
            [FromQuery] string? clitel = null,
            [FromQuery] string? mail = null,
            [FromQuery] string? cliruc = null,
            [FromQuery] bool soloActivos = true)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} obteniendo clientes - Página: {Page}, Tamaño: {PageSize}",
                    userId, page, pageSize);

                ClienteSearchFilters? filters = null;
                if (!string.IsNullOrEmpty(nombre) || !string.IsNullOrEmpty(cliced) ||
                    !string.IsNullOrEmpty(clicel) || !string.IsNullOrEmpty(clitel) ||
                    !string.IsNullOrEmpty(mail) || !string.IsNullOrEmpty(cliruc))
                {
                    filters = new ClienteSearchFilters
                    {
                        Nombre = nombre,
                        Cliced = cliced,
                        Clicel = clicel,
                        Clitel = clitel,
                        Mail = mail,
                        Cliruc = cliruc,
                        SoloActivos = soloActivos
                    };
                    filters.TrimAndCleanFilters();
                }

                var result = await _masterDataService.GetClientesPaginatedAsync(page, pageSize, filters);

                var response = new
                {
                    data = result.Items,
                    pagination = new
                    {
                        page = result.Page,
                        pageSize = result.PageSize,
                        count = result.Count,
                        totalCount = result.TotalCount,
                        totalPages = result.TotalPages,
                        hasNextPage = result.HasNextPage,
                        hasPreviousPage = result.HasPreviousPage,
                        startIndex = result.StartIndex,
                        endIndex = result.EndIndex
                    },
                    filters = new
                    {
                        applied = filters != null,
                        nombre,
                        cliced,
                        clicel,
                        clitel,
                        mail,
                        cliruc,
                        soloActivos
                    },
                    metadata = new
                    {
                        timestamp = DateTime.UtcNow,
                        requestDuration = "calculated_on_frontend"
                    }
                };

                _logger.LogInformation("Clientes obtenidos: {Count}/{TotalCount} en página {Page}",
                    result.Count, result.TotalCount, page);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Parámetros inválidos: {Error}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conectividad obteniendo clientes");
                return StatusCode(503, new { message = "Servicio Velneo temporalmente no disponible" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado obteniendo clientes");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("clientes/search")]
        [ProducesResponseType(typeof(ApiResponse<List<ClienteItem>>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<List<ClienteItem>>>> SearchClientesQuick(
            [FromQuery] string? query,
            [FromQuery] int limit = 10)
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

                if (limit < 1) limit = 10;
                if (limit > 50) limit = 50;

                var userId = GetCurrentUserId();
                _logger.LogInformation("Usuario {UserId} búsqueda rápida: '{Query}' (limit: {Limit})",
                    userId, query, limit);

                var clientes = await _masterDataService.SearchClientesQuickAsync(query, limit);

                var message = clientes.Count switch
                {
                    0 => $"No se encontraron clientes para '{query}'",
                    1 => "Se encontró 1 cliente",
                    _ => $"Se encontraron {clientes.Count} clientes"
                };

                _logger.LogInformation("Búsqueda rápida completada: {Count} resultados para '{Query}'",
                    clientes.Count, query);

                return Ok(ApiResponse<List<ClienteItem>>.SuccessResult(clientes, message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Parámetros inválidos en búsqueda rápida: {Error}", ex.Message);
                return BadRequest(ApiResponse<List<ClienteItem>>.ErrorResult(ex.Message));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conectividad en búsqueda rápida para query '{Query}'", query);
                return StatusCode(503, ApiResponse<List<ClienteItem>>.ErrorResult(
                    "Servicio Velneo temporalmente no disponible"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en búsqueda rápida para query '{Query}'", query);
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
                _logger.LogInformation("Usuario {UserId} obteniendo detalle cliente {ClienteId}",
                    userId, clienteId);

                var cliente = await _masterDataService.GetClienteDetalleAsync(clienteId);

                if (cliente == null)
                {
                    _logger.LogWarning("Cliente {ClienteId} no encontrado en Velneo", clienteId);
                    return NotFound(ApiResponse<ClienteItem>.ErrorResult(
                        $"Cliente con ID {clienteId} no encontrado"));
                }

                var message = cliente.activo
                    ? "Detalle del cliente obtenido exitosamente"
                    : "Cliente encontrado pero está marcado como inactivo";

                _logger.LogInformation("Cliente {ClienteId} obtenido: '{DisplayName}' (Activo: {Activo})",
                    clienteId, cliente.DisplayName, cliente.activo);

                return Ok(ApiResponse<ClienteItem>.SuccessResult(cliente, message));
            }
            catch (FormatException ex)
            {
                _logger.LogWarning("ID de cliente inválido: {ClienteId}", clienteId);
                return BadRequest(ApiResponse<ClienteItem>.ErrorResult("ID de cliente inválido"));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conectividad obteniendo cliente {ClienteId}", clienteId);
                return StatusCode(503, ApiResponse<ClienteItem>.ErrorResult(
                    "Servicio Velneo temporalmente no disponible"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado obteniendo detalle cliente {ClienteId}", clienteId);
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
                _logger.LogError(ex, "Error obteniendo departamentos");
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
                _logger.LogError(ex, "Error obteniendo combustibles");
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
                _logger.LogError(ex, "Error obteniendo corredores");
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
                _logger.LogError(ex, "Error obteniendo categorías");
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
                _logger.LogError(ex, "Error obteniendo destinos");
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
                _logger.LogError(ex, "Error obteniendo calidades");
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
                _logger.LogError(ex, "Error obteniendo tarifas");
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
                _logger.LogInformation("Usuario {UserId} obteniendo compañías", userId);

                var companias = await _masterDataService.GetCompaniasAsync();

                _logger.LogInformation("Compañías obtenidas: {Count}", companias.Count);

                return Ok(ApiResponse<List<CompaniaItem>>.SuccessResult(
                    companias,
                    $"Se encontraron {companias.Count} compañías"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo compañías");
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
                _logger.LogInformation("Usuario {UserId} obteniendo secciones (compañía: {CompaniaId})",
                    userId, companiaId?.ToString() ?? "todas");

                var secciones = await _masterDataService.GetSeccionesAsync(companiaId);

                _logger.LogInformation("Secciones obtenidas: {Count}", secciones.Count);

                return Ok(ApiResponse<List<SeccionItem>>.SuccessResult(
                    secciones,
                    $"Se encontraron {secciones.Count} secciones"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo secciones");
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
                _logger.LogError(ex, "Error obteniendo monedas");
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
                _logger.LogError(ex, "Health check falló");
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