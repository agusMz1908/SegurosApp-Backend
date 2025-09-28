using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Models;
using SegurosApp.API.Services.Poliza.Shared;
using System.Text.Json;

namespace SegurosApp.API.Services.Poliza
{
    public class ModifyPolizaService
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly PolizaDataExtractor _dataExtractor;
        private readonly ObservationsGenerator _observationsGenerator;
        private readonly AppDbContext _context;
        private readonly ILogger<ModifyPolizaService> _logger;

        public ModifyPolizaService(
            IVelneoMasterDataService masterDataService,
            PolizaDataExtractor dataExtractor,
            ObservationsGenerator observationsGenerator,
            AppDbContext context,
            ILogger<ModifyPolizaService> logger)
        {
            _masterDataService = masterDataService;
            _dataExtractor = dataExtractor;
            _observationsGenerator = observationsGenerator;
            _context = context;
            _logger = logger;
        }

        public async Task<VelneoPolizaRequest> CreateVelneoRequestFromModifyAsync(
            int scanId,
            int userId,
            ModifyPolizaRequest modifyRequest)
        {
            _logger.LogInformation("Creando request Velneo para cambio de póliza - Scan: {ScanId}, Usuario: {UserId}, Póliza anterior: {PolizaAnteriorId}",
                scanId, userId, modifyRequest.PolizaAnteriorId);

            var scan = await GetScanForModify(scanId, userId);
            var extractedData = DeserializeExtractedData(scan.ExtractedData);
            var context = GetModifyContext(scan);
            var contextInfo = await GetContextInformation(context);
            var modifyData = ProcessModifySpecificData(modifyRequest, extractedData);
            var request = BuildBaseModifyRequest(context, contextInfo, modifyData);

            await ApplyModifyMasterDataOverrides(request, modifyRequest);
            ConfigureForModify(request, modifyRequest);
            var cambiosDetectados = DetectarCambios(modifyRequest, extractedData);

            _logger.LogInformation("=== PUNTO CRÍTICO === Generando observaciones. Observaciones actuales: '{ObservacionesActuales}'", request.observaciones ?? "NULL");

            request.observaciones = _observationsGenerator.GenerateModifyPolizaObservations(
                modifyRequest.PolizaAnteriorNumero ?? "",
                modifyRequest.PolizaAnteriorId,
                modifyRequest.TipoCambio,
                request.concuo,
                request.contot,
                request.confchdes,
                modifyRequest.Observaciones,
                modifyRequest.ComentariosUsuario,
                cambiosDetectados);

            _logger.LogInformation("=== DESPUÉS GENERADOR === Observaciones: '{ObservacionesGeneradas}'", request.observaciones);

            await ValidateModifyRequest(request, modifyRequest);
            return request;
        }

        public async Task<ModifyValidationResult> ValidateModifyDataAsync(
            int scanId,
            int userId,
            ModifyPolizaRequest modifyRequest)
        {
            var result = new ModifyValidationResult();

            try
            {
                var scan = await _context.DocumentScans
                    .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId);

                if (scan == null)
                {
                    result.AddError("Scan no encontrado o no pertenece al usuario");
                    return result;
                }

                var polizaAnterior = await _masterDataService.GetPolizaDetalleAsync(modifyRequest.PolizaAnteriorId);
                if (polizaAnterior == null)
                {
                    result.AddError($"Póliza anterior {modifyRequest.PolizaAnteriorId} no encontrada");
                    return result;
                }

                if (!scan.ClienteId.HasValue || !scan.CompaniaId.HasValue || !scan.SeccionId.HasValue)
                {
                    result.AddError("El scan no tiene contexto de cliente, compañía o sección");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(modifyRequest.TipoCambio))
                {
                    result.AddError("Tipo de cambio es requerido");
                    return result;
                }

                if (!polizaAnterior.EsVigente)
                {
                    result.AddWarning($"La póliza anterior no está vigente (Estado: {polizaAnterior.EstadoDisplay})");
                }

                if (modifyRequest.Premio.HasValue && modifyRequest.Premio.Value <= 0)
                {
                    result.AddWarning("Premio especificado es menor o igual a cero");
                }

                if (modifyRequest.MontoTotal.HasValue && modifyRequest.MontoTotal.Value <= 0)
                {
                    result.AddWarning("Monto total especificado es menor o igual a cero");
                }

                result.IsValid = !result.Errors.Any();
                result.PolizaAnterior = polizaAnterior;

                _logger.LogInformation("Validación de cambio completada - Válida: {IsValid}, Errores: {ErrorCount}, Advertencias: {WarningCount}",
                    result.IsValid, result.Errors.Count, result.Warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando datos de cambio de póliza");
                result.AddError($"Error inesperado durante validación: {ex.Message}");
                return result;
            }
        }

        #region Métodos Privados

        private async Task<DocumentScan> GetScanForModify(int scanId, int userId)
        {
            var scan = await _context.DocumentScans
                .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId);

            if (scan == null)
            {
                throw new ArgumentException($"Scan {scanId} no encontrado para usuario {userId}");
            }

            return scan;
        }

        private Dictionary<string, object> DeserializeExtractedData(string extractedDataJson)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(extractedDataJson)
                ?? new Dictionary<string, object>();
        }

        private ModifyContext GetModifyContext(DocumentScan scan)
        {
            return new ModifyContext
            {
                ScanId = scan.Id,
                ClienteId = scan.ClienteId ?? throw new ArgumentException("ClienteId requerido para cambio"),
                CompaniaId = scan.CompaniaId ?? throw new ArgumentException("CompaniaId requerido para cambio"),
                SeccionId = scan.SeccionId ?? throw new ArgumentException("SeccionId requerido para cambio"),
                UserId = scan.UserId
            };
        }

        private async Task<ModifyContextInfo> GetContextInformation(ModifyContext context)
        {
            var contextInfo = new ModifyContextInfo();

            try
            {
                contextInfo.Cliente = await _masterDataService.GetClienteDetalleAsync(context.ClienteId);

                var companias = await _masterDataService.GetCompaniasAsync();
                contextInfo.Compania = companias.FirstOrDefault(c => c.id == context.CompaniaId);

                var secciones = await _masterDataService.GetSeccionesAsync(context.CompaniaId);
                contextInfo.Seccion = secciones.FirstOrDefault(s => s.id == context.SeccionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error obteniendo información de contexto para cambio: {Error}", ex.Message);
            }

            return contextInfo;
        }

        private ModifySpecificData ProcessModifySpecificData(
            ModifyPolizaRequest modifyRequest,
            Dictionary<string, object> extractedData)
        {
            var data = new ModifySpecificData();

            data.FechaDesde = modifyRequest.FechaDesde ?? _dataExtractor.ExtractStartDate(extractedData);
            data.FechaHasta = modifyRequest.FechaHasta ?? _dataExtractor.ExtractEndDate(extractedData);

            data.Premio = modifyRequest.Premio ?? _dataExtractor.ExtractPremium(extractedData);
            data.MontoTotal = modifyRequest.MontoTotal ?? _dataExtractor.ExtractTotalAmount(extractedData);
            data.CantidadCuotas = modifyRequest.CantidadCuotas ?? _dataExtractor.ExtractInstallmentCount(extractedData);

            data.VehiculoMarca = modifyRequest.VehiculoMarca ?? _dataExtractor.ExtractVehicleBrand(extractedData);
            data.VehiculoModelo = modifyRequest.VehiculoModelo ?? _dataExtractor.ExtractVehicleModel(extractedData);
            data.VehiculoAno = modifyRequest.VehiculoAno ?? _dataExtractor.ExtractVehicleYear(extractedData);
            data.VehiculoMotor = modifyRequest.VehiculoMotor ?? _dataExtractor.ExtractMotorNumber(extractedData);
            data.VehiculoChasis = modifyRequest.VehiculoChasis ?? _dataExtractor.ExtractChassisNumber(extractedData);
            data.VehiculoPatente = modifyRequest.VehiculoPatente ?? _dataExtractor.ExtractVehiclePlate(extractedData);

            data.NumeroPoliza = modifyRequest.NumeroPoliza ?? _dataExtractor.ExtractPolicyNumber(extractedData);

            return data;
        }

        private VelneoPolizaRequest BuildBaseModifyRequest(
            ModifyContext context,
            ModifyContextInfo contextInfo,
            ModifySpecificData data)
        {
            var formattedStartDate = ConvertToVelneoDateFormat(data.FechaDesde);
            var formattedEndDate = ConvertToVelneoDateFormat(data.FechaHasta);

            return new VelneoPolizaRequest
            {
                clinro = context.ClienteId,
                comcod = context.CompaniaId,
                seccod = context.SeccionId,
                conpol = data.NumeroPoliza,
                conend = _dataExtractor.ExtractEndorsement(new Dictionary<string, object>()),
                confchdes = formattedStartDate,
                confchhas = formattedEndDate,
                conpremio = (int)Math.Round(data.Premio),
                contot = (int)Math.Round(data.MontoTotal),
                conmaraut = data.VehiculoMarca,
                conmodaut = data.VehiculoModelo,
                conanioaut = data.VehiculoAno,
                conmotor = data.VehiculoMotor,
                conchasis = data.VehiculoChasis,
                conmataut = data.VehiculoPatente,
                clinom = contextInfo.Cliente?.clinom ?? "",
                condom = contextInfo.Cliente?.clidir ?? "",
                clinro1 = 0,
                consta = "1", 
                concuo = data.CantidadCuotas,
                moncod = 1, 
                conviamon = 1,
                congesti = "1",
                congeses = "5",
                contra = "3",   
                convig = "1",
                com_alias = contextInfo.Compania?.comnom ?? "",
                ramo = contextInfo.Seccion?.seccion ?? "",
                ingresado = DateTime.UtcNow,
                last_update = DateTime.UtcNow,
                app_id = context.ScanId,
                conpadre = 0 
            };
        }

        private async Task ApplyModifyMasterDataOverrides(VelneoPolizaRequest request, ModifyPolizaRequest modifyRequest)
        {
            _logger.LogInformation("Aplicando overrides de master data para cambio - Combustible: {Combustible}, Categoría: {Categoria}",
                modifyRequest.CombustibleId, modifyRequest.CategoriaId);

            if (modifyRequest.DepartamentoId.HasValue && modifyRequest.DepartamentoId.Value > 0)
                request.dptnom = modifyRequest.DepartamentoId.Value;

            if (!string.IsNullOrEmpty(modifyRequest.CombustibleId))
                request.combustibles = modifyRequest.CombustibleId;

            if (modifyRequest.DestinoId.HasValue && modifyRequest.DestinoId.Value > 0)
                request.desdsc = modifyRequest.DestinoId.Value;

            if (modifyRequest.CategoriaId.HasValue && modifyRequest.CategoriaId.Value > 0)
                request.catdsc = modifyRequest.CategoriaId.Value;

            if (modifyRequest.CalidadId.HasValue && modifyRequest.CalidadId.Value > 0)
                request.caldsc = modifyRequest.CalidadId.Value;

            if (modifyRequest.TarifaId.HasValue && modifyRequest.TarifaId.Value > 0)
                request.tarcod = modifyRequest.TarifaId.Value;

            if (modifyRequest.CorredorId.HasValue && modifyRequest.CorredorId.Value > 0)
                request.corrnom = modifyRequest.CorredorId.Value;

            if (modifyRequest.MonedaId.HasValue && modifyRequest.MonedaId.Value > 0)
            {
                request.moncod = modifyRequest.MonedaId.Value;
                request.conviamon = modifyRequest.MonedaId.Value;
            }

            _logger.LogInformation("Master data aplicado para cambio - Combustible: {Combustible}, Categoría: {Categoria}, Calidad: {Calidad}",
                request.combustibles, request.catdsc, request.caldsc);
        }

        private void ConfigureForModify(VelneoPolizaRequest request, ModifyPolizaRequest modifyRequest)
        {
            request.conpadre = modifyRequest.PolizaAnteriorId;
            request.contra = "3";
            request.congeses = "5"; 
            request.convig = "1"; 

            _logger.LogInformation("Request configurado para cambio - ConPadre: {ConPadre}, Tipo: Cambio (3), TipoCambio: {TipoCambio}",
                modifyRequest.PolizaAnteriorId, modifyRequest.TipoCambio);
        }

        private Dictionary<string, string> DetectarCambios(ModifyPolizaRequest modifyRequest, Dictionary<string, object> extractedData)
        {
            var cambios = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(modifyRequest.VehiculoMarca))
            {
                cambios.Add("Marca del vehículo", modifyRequest.VehiculoMarca);
            }

            if (!string.IsNullOrEmpty(modifyRequest.VehiculoModelo))
            {
                cambios.Add("Modelo del vehículo", modifyRequest.VehiculoModelo);
            }

            if (modifyRequest.VehiculoAno.HasValue && modifyRequest.VehiculoAno > 0)
            {
                cambios.Add("Año del vehículo", modifyRequest.VehiculoAno.Value.ToString());
            }

            if (modifyRequest.Premio.HasValue && modifyRequest.Premio > 0)
            {
                cambios.Add("Premio", $"${modifyRequest.Premio.Value:N2}");
            }

            if (modifyRequest.MontoTotal.HasValue && modifyRequest.MontoTotal > 0)
            {
                cambios.Add("Monto total", $"${modifyRequest.MontoTotal.Value:N2}");
            }

            if (modifyRequest.CantidadCuotas.HasValue && modifyRequest.CantidadCuotas > 0)
            {
                cambios.Add("Cantidad de cuotas", modifyRequest.CantidadCuotas.Value.ToString());
            }

            return cambios;
        }

        private async Task ValidateModifyRequest(VelneoPolizaRequest request, ModifyPolizaRequest modifyRequest)
        {
            var errors = new List<string>();

            if (request.clinro <= 0)
                errors.Add("Cliente ID es requerido");
            if (request.comcod <= 0)
                errors.Add("Compañía ID es requerido");
            if (request.seccod <= 0)
                errors.Add("Sección ID es requerido");
            if (string.IsNullOrEmpty(request.conpol))
                errors.Add("Número de póliza es requerido");
            if (request.conpadre <= 0)
                errors.Add("ID de póliza anterior es requerido para cambio");

            if (errors.Any())
            {
                throw new ValidationException($"Errores de validación en cambio: {string.Join(", ", errors)}");
            }

            _logger.LogInformation("Request de cambio validado exitosamente");
        }

        private string ConvertToVelneoDateFormat(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
            {
                return DateTime.Today.ToString("yyyy-MM-dd");
            }

            try
            {
                var cleanDate = dateStr.Trim();
                if (DateTime.TryParseExact(cleanDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var alreadyFormatted))
                {
                    return cleanDate;
                }

                var formats = new[]
                {
                    "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "yyyy/MM/dd",
                    "dd/MM/yy", "MM/dd/yy", "yyyyMMdd", "dd.MM.yyyy"
                };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(cleanDate, format,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        return parsedDate.ToString("yyyy-MM-dd");
                    }
                }

                if (DateTime.TryParse(cleanDate, out var flexibleDate))
                {
                    return flexibleDate.ToString("yyyy-MM-dd");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parseando fecha '{DateStr}'", dateStr);
            }

            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        #endregion
    }

    #region DTOs Auxiliares

    public class ModifyContext
    {
        public int ScanId { get; set; }
        public int ClienteId { get; set; }
        public int CompaniaId { get; set; }
        public int SeccionId { get; set; }
        public int UserId { get; set; }
    }

    public class ModifyContextInfo
    {
        public SegurosApp.API.DTOs.Velneo.Item.ClienteItem? Cliente { get; set; }
        public SegurosApp.API.DTOs.Velneo.Item.CompaniaItem? Compania { get; set; }
        public SegurosApp.API.DTOs.Velneo.Item.SeccionItem? Seccion { get; set; }
    }

    public class ModifySpecificData
    {
        public string FechaDesde { get; set; } = "";
        public string FechaHasta { get; set; } = "";
        public decimal Premio { get; set; }
        public decimal MontoTotal { get; set; }
        public int CantidadCuotas { get; set; } = 1;
        public string NumeroPoliza { get; set; } = "";
        public string VehiculoMarca { get; set; } = "";
        public string VehiculoModelo { get; set; } = "";
        public int VehiculoAno { get; set; }
        public string VehiculoMotor { get; set; } = "";
        public string VehiculoChasis { get; set; } = "";
        public string VehiculoPatente { get; set; } = "";
    }

    public class ModifyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public ContratoItem? PolizaAnterior { get; set; }

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}