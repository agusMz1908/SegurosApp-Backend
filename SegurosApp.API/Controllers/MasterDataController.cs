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
        public async Task<ActionResult<CreatePolizaResponse>> CreatePoliza(
            [FromBody] CreatePolizaRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                _logger.LogInformation("🚀 Usuario {UserId} creando póliza para scan {ScanId}",
                    userId, request.ScanId);

                var result = await _masterDataService.CreatePolizaAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("✅ Póliza creada exitosamente: {PolizaId} - {PolizaNumber}",
                        result.PolizaId, result.PolizaNumber);
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

        // ✅ ENDPOINTS INDIVIDUALES DE MASTER DATA
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
    }

    // REQUEST DTOS
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