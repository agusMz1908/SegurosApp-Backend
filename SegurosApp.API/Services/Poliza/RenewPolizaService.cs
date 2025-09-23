using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Models;
using SegurosApp.API.Services.Poliza.Shared;
using System.Text.Json;

namespace SegurosApp.API.Services.Poliza
{
    public class RenewPolizaService
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly PolizaDataExtractor _dataExtractor;
        private readonly ObservationsGenerator _observationsGenerator;
        private readonly AppDbContext _context;
        private readonly ILogger<RenewPolizaService> _logger;

        public RenewPolizaService(
            IVelneoMasterDataService masterDataService,
            PolizaDataExtractor dataExtractor,
            ObservationsGenerator observationsGenerator,
            AppDbContext context,
            ILogger<RenewPolizaService> logger)
        {
            _masterDataService = masterDataService;
            _dataExtractor = dataExtractor;
            _observationsGenerator = observationsGenerator;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea un request de Velneo para renovación de póliza
        /// </summary>
        public async Task<VelneoPolizaRequest> CreateVelneoRequestFromRenewAsync(
            int scanId,
            int userId,
            RenewPolizaRequest renewRequest,
            object? polizaAnterior = null)
        {
            _logger.LogInformation("Creando request Velneo para renovación - Scan: {ScanId}, Usuario: {UserId}, PolizaAnterior: {PolizaAnteriorId}",
                scanId, userId, renewRequest.PolizaAnteriorId);

            // Obtener el scan
            var scan = await GetScanForRenewal(scanId, userId);
            var extractedData = DeserializeExtractedData(scan.ExtractedData);
            var normalizedData = await NormalizeDataForRenewal(extractedData, scan.CompaniaId);

            // Obtener contexto del scan
            var context = GetRenewalContext(scan);

            // Obtener información del contexto (cliente, compañía, sección)
            var contextInfo = await GetContextInformation(context);

            // Procesar datos específicos de renovación
            var renewalData = ProcessRenewalSpecificData(renewRequest, normalizedData);

            // Construir el request base
            var request = BuildBaseRenewalRequest(context, contextInfo, renewalData);

            // Aplicar master data del frontend (prioritario)
            await ApplyFrontendMasterDataOverrides(request, renewRequest);

            // Configurar para renovación
            ConfigureForRenewal(request, renewRequest.PolizaAnteriorId, renewalData);

            // Generar observaciones específicas para renovación
            var polizaAnteriorInfo = ExtractPolizaAnteriorInfo(polizaAnterior);
            request.observaciones = _observationsGenerator.GenerateRenewPolizaObservations(
                polizaAnteriorInfo.NumeroPoliza,
                renewRequest.PolizaAnteriorId,
                request.concuo,
                request.contot,
                request.confchdes,
                polizaAnteriorInfo.FechaVencimiento,
                renewRequest.Observaciones,
                renewRequest.ComentariosUsuario);

            _logger.LogInformation("Request Velneo para renovación creado exitosamente");

            await ValidateRenewalRequest(request);
            return request;
        }

        /// <summary>
        /// Valida los datos necesarios para renovación
        /// </summary>
        public async Task<RenewalValidationResult> ValidateRenewalDataAsync(
            int scanId,
            int userId,
            RenewPolizaRequest renewRequest)
        {
            var result = new RenewalValidationResult();

            try
            {
                // Validar que existe el scan
                var scan = await _context.DocumentScans
                    .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId);

                if (scan == null)
                {
                    result.AddError("Scan no encontrado o no pertenece al usuario");
                    return result;
                }

                // Validar póliza anterior
                var polizaAnterior = await _masterDataService.GetPolizaDetalleAsync(renewRequest.PolizaAnteriorId);
                if (polizaAnterior == null)
                {
                    result.AddError($"Póliza anterior {renewRequest.PolizaAnteriorId} no encontrada");
                    return result;
                }

                // Validar contexto del scan
                if (!scan.ClienteId.HasValue || !scan.CompaniaId.HasValue || !scan.SeccionId.HasValue)
                {
                    result.AddError("El scan no tiene contexto de cliente, compañía o sección");
                    return result;
                }

                // Validar fechas de renovación
                if (renewRequest.ValidarVencimiento)
                {
                    var dateValidation = ValidateRenewalDates(polizaAnterior, renewRequest);
                    if (!dateValidation.IsValid)
                    {
                        result.AddError(dateValidation.ErrorMessage);
                        result.FechaVencimientoAnterior = dateValidation.FechaVencimiento;
                    }
                }

                // Validar datos financieros
                if (renewRequest.Premio.HasValue && renewRequest.Premio.Value <= 0)
                {
                    result.AddWarning("Premio especificado es menor o igual a cero");
                }

                if (renewRequest.MontoTotal.HasValue && renewRequest.MontoTotal.Value <= 0)
                {
                    result.AddWarning("Monto total especificado es menor o igual a cero");
                }

                // Validar cuotas
                if (renewRequest.CantidadCuotas.HasValue &&
                    (renewRequest.CantidadCuotas.Value < 1 || renewRequest.CantidadCuotas.Value > 60))
                {
                    result.AddWarning("Cantidad de cuotas fuera del rango normal (1-60)");
                }

                result.IsValid = !result.Errors.Any();
                result.PolizaAnterior = polizaAnterior;

                _logger.LogInformation("Validación de renovación completada - Válida: {IsValid}, Errores: {ErrorCount}, Advertencias: {WarningCount}",
                    result.IsValid, result.Errors.Count, result.Warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando datos de renovación");
                result.AddError($"Error inesperado durante validación: {ex.Message}");
                return result;
            }
        }

        #region Métodos Privados

        private async Task<DocumentScan> GetScanForRenewal(int scanId, int userId)
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

        private async Task<Dictionary<string, object>> NormalizeDataForRenewal(
            Dictionary<string, object> extractedData,
            int? companiaId)
        {
            // Por ahora usar normalización básica, se puede extender específicamente para renovaciones
            return extractedData;
        }

        private RenewalContext GetRenewalContext(DocumentScan scan)
        {
            return new RenewalContext
            {
                ScanId = scan.Id,
                ClienteId = scan.ClienteId ?? throw new ArgumentException("ClienteId requerido para renovación"),
                CompaniaId = scan.CompaniaId ?? throw new ArgumentException("CompaniaId requerido para renovación"),
                SeccionId = scan.SeccionId ?? throw new ArgumentException("SeccionId requerido para renovación"),
                UserId = scan.UserId
            };
        }

        private async Task<RenewalContextInfo> GetContextInformation(RenewalContext context)
        {
            var contextInfo = new RenewalContextInfo();

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
                _logger.LogWarning("Error obteniendo información de contexto para renovación: {Error}", ex.Message);
            }

            return contextInfo;
        }

        private RenewalSpecificData ProcessRenewalSpecificData(
            RenewPolizaRequest renewRequest,
            Dictionary<string, object> normalizedData)
        {
            var data = new RenewalSpecificData();

            // Fechas (prioritizar frontend, fallback a extraído)
            data.FechaDesde = GetStringValueWithRenewOverride(
                renewRequest.FechaDesde,
                _dataExtractor.ExtractStartDate(normalizedData),
                "FechaInicio");

            data.FechaHasta = GetStringValueWithRenewOverride(
                renewRequest.FechaHasta,
                _dataExtractor.ExtractEndDate(normalizedData),
                "FechaFin");

            // Montos (prioritizar frontend, fallback a extraído)
            data.Premio = renewRequest.Premio ?? _dataExtractor.ExtractPremium(normalizedData);
            data.MontoTotal = renewRequest.MontoTotal ?? _dataExtractor.ExtractTotalAmount(normalizedData);
            data.CantidadCuotas = renewRequest.CantidadCuotas ?? _dataExtractor.ExtractInstallmentCount(normalizedData);

            // Datos del vehículo (prioritizar frontend, fallback a extraído)
            data.VehiculoMarca = renewRequest.VehiculoMarca ?? _dataExtractor.ExtractVehicleBrand(normalizedData);
            data.VehiculoModelo = renewRequest.VehiculoModelo ?? _dataExtractor.ExtractVehicleModel(normalizedData);
            data.VehiculoAno = renewRequest.VehiculoAno ?? _dataExtractor.ExtractVehicleYear(normalizedData);
            data.VehiculoMotor = renewRequest.VehiculoMotor ?? _dataExtractor.ExtractMotorNumber(normalizedData);
            data.VehiculoChasis = renewRequest.VehiculoChasis ?? _dataExtractor.ExtractChassisNumber(normalizedData);
            data.VehiculoPatente = renewRequest.VehiculoPatente ?? _dataExtractor.ExtractVehiclePlate(normalizedData);

            // Número de póliza
            data.NumeroPoliza = renewRequest.NumeroPoliza ?? _dataExtractor.ExtractPolicyNumber(normalizedData);

            return data;
        }

        private VelneoPolizaRequest BuildBaseRenewalRequest(
            RenewalContext context,
            RenewalContextInfo contextInfo,
            RenewalSpecificData data)
        {
            var formattedStartDate = ConvertToVelneoDateFormat(data.FechaDesde);
            var formattedEndDate = ConvertToVelneoDateFormat(data.FechaHasta);

            return new VelneoPolizaRequest
            {
                // IDs principales
                clinro = context.ClienteId,
                comcod = context.CompaniaId,
                seccod = context.SeccionId,

                // Datos de póliza
                conpol = data.NumeroPoliza,
                conend = _dataExtractor.ExtractEndorsement(new Dictionary<string, object>()),
                confchdes = formattedStartDate,
                confchhas = formattedEndDate,
                conpremio = (int)Math.Round(data.Premio),
                contot = (int)Math.Round(data.MontoTotal),

                // Datos del vehículo
                conmaraut = data.VehiculoMarca,
                conmodaut = data.VehiculoModelo,
                conanioaut = data.VehiculoAno,
                conmotor = data.VehiculoMotor,
                conchasis = data.VehiculoChasis,
                conmataut = data.VehiculoPatente,

                // Datos del cliente
                clinom = contextInfo.Cliente?.clinom ?? "",
                condom = contextInfo.Cliente?.clidir ?? "",
                clinro1 = 0, // Beneficiario

                // Condiciones de pago
                consta = "1", // Método de pago por defecto
                concuo = data.CantidadCuotas,
                moncod = 1, // Moneda por defecto (UYU)
                conviamon = 1,

                // Estados por defecto
                congesti = "1",
                congeses = "1",
                contra = "1",
                convig = "1",

                // Información adicional
                com_alias = contextInfo.Compania?.comnom ?? "",
                ramo = contextInfo.Seccion?.seccion ?? "",

                // Metadatos
                ingresado = DateTime.UtcNow,
                last_update = DateTime.UtcNow,
                app_id = context.ScanId,
                conpadre = 0 // Se establecerá en ConfigureForRenewal
            };
        }

        private async Task ApplyFrontendMasterDataOverrides(VelneoPolizaRequest request, RenewPolizaRequest renewRequest)
        {
            _logger.LogInformation("Aplicando overrides de master data del frontend - Combustible: {Combustible}, Categoría: {Categoria}",
                renewRequest.CombustibleId, renewRequest.CategoriaId);

            // Master data con prioridad al frontend
            request.dptnom = await GetIntValueWithRenewOverrideAsync(renewRequest.DepartamentoId, () => Task.FromResult(1));
            request.combustibles = await GetStringValueWithRenewOverrideAsync(renewRequest.CombustibleId, () => Task.FromResult("1"));
            request.desdsc = await GetIntValueWithRenewOverrideAsync(renewRequest.DestinoId, () => Task.FromResult(1));
            request.catdsc = await GetIntValueWithRenewOverrideAsync(renewRequest.CategoriaId, () => Task.FromResult(1));
            request.caldsc = await GetIntValueWithRenewOverrideAsync(renewRequest.CalidadId, () => Task.FromResult(1));
            request.tarcod = await GetIntValueWithRenewOverrideAsync(renewRequest.TarifaId, () => Task.FromResult(1));
            request.corrnom = GetIntValueWithRenewOverride(renewRequest.CorredorId, 0);
            request.moncod = GetIntValueWithRenewOverride(renewRequest.MonedaId, 1);
            request.conviamon = GetIntValueWithRenewOverride(renewRequest.MonedaId, 1);

            _logger.LogInformation("Master data aplicado - Combustible: {Combustible}, Categoría: {Categoria}, Calidad: {Calidad}",
                request.combustibles, request.catdsc, request.caldsc);
        }

        private void ConfigureForRenewal(VelneoPolizaRequest request, int polizaAnteriorId, RenewalSpecificData data)
        {
            // Configurar como renovación
            request.conpadre = polizaAnteriorId;
            request.contra = "2"; // Código para renovación
            request.congeses = "1"; // Estado de gestión activo
            request.convig = "1"; // Vigente

            _logger.LogInformation("Request configurado para renovación - ConPadre: {ConPadre}, Tipo: Renovación (2)", polizaAnteriorId);
        }

        private PolizaAnteriorInfo ExtractPolizaAnteriorInfo(object? polizaAnterior)
        {
            var info = new PolizaAnteriorInfo();

            if (polizaAnterior == null) return info;

            try
            {
                var type = polizaAnterior.GetType();

                var conpolProperty = type.GetProperty("conpol");
                info.NumeroPoliza = conpolProperty?.GetValue(polizaAnterior)?.ToString() ?? "";

                var fechaHastaProperty = type.GetProperty("confchhas");
                var fechaHastaStr = fechaHastaProperty?.GetValue(polizaAnterior)?.ToString();
                if (DateTime.TryParse(fechaHastaStr, out var fechaVencimiento))
                {
                    info.FechaVencimiento = fechaVencimiento;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extrayendo información de póliza anterior: {Error}", ex.Message);
            }

            return info;
        }

        private RenewalDateValidationResult ValidateRenewalDates(object polizaAnterior, RenewPolizaRequest renewRequest)
        {
            var result = new RenewalDateValidationResult { IsValid = true };

            try
            {
                // Extraer fecha de vencimiento de la póliza anterior
                var type = polizaAnterior.GetType();
                var fechaHastaProperty = type.GetProperty("confchhas");
                var fechaHastaStr = fechaHastaProperty?.GetValue(polizaAnterior)?.ToString();

                if (DateTime.TryParse(fechaHastaStr, out var fechaVencimiento))
                {
                    result.FechaVencimiento = fechaVencimiento;
                    var diasParaVencimiento = (fechaVencimiento - DateTime.Now).Days;

                    _logger.LogInformation("Póliza vence el {FechaVencimiento}, días restantes: {Dias}",
                        fechaVencimiento.ToString("dd/MM/yyyy"), diasParaVencimiento);

                    if (diasParaVencimiento > 60)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"La póliza vence el {fechaVencimiento:dd/MM/yyyy}. Solo se puede renovar hasta 60 días antes del vencimiento.";
                    }
                    else if (diasParaVencimiento < -30)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"La póliza venció el {fechaVencimiento:dd/MM/yyyy}. No se puede renovar una póliza vencida hace más de 30 días.";
                    }
                }
                else
                {
                    _logger.LogWarning("No se pudo parsear fecha de vencimiento de póliza anterior: {FechaStr}", fechaHastaStr);
                    // En caso de no poder validar la fecha, permitir continuar
                    result.IsValid = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando fechas de renovación");
                result.IsValid = true; // Permitir continuar en caso de error
            }

            return result;
        }

        private async Task ValidateRenewalRequest(VelneoPolizaRequest request)
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
                errors.Add("ID de póliza anterior es requerido para renovación");

            if (errors.Any())
            {
                throw new ValidationException($"Errores de validación en renovación: {string.Join(", ", errors)}");
            }

            _logger.LogInformation("Request de renovación validado exitosamente");
        }

        #endregion

        #region Métodos de Utilidad

        private string GetStringValueWithRenewOverride(string? frontendValue, string extractedValue, string fieldName)
        {
            if (!string.IsNullOrEmpty(frontendValue))
            {
                _logger.LogDebug("Usando override de frontend para {FieldName}: {Value}", fieldName, frontendValue);
                return frontendValue;
            }
            return extractedValue ?? "";
        }

        private int GetIntValueWithRenewOverride(string? frontendValue, int extractedValue)
        {
            if (!string.IsNullOrEmpty(frontendValue) && int.TryParse(frontendValue, out var parsedValue))
            {
                _logger.LogDebug("Usando override numérico de frontend: {Value}", parsedValue);
                return parsedValue;
            }
            return extractedValue;
        }

        private async Task<string> GetStringValueWithRenewOverrideAsync(string? frontendValue, Func<Task<string>> extractFunc)
        {
            if (!string.IsNullOrEmpty(frontendValue))
            {
                _logger.LogDebug("Usando override async string de frontend: {Value}", frontendValue);
                return frontendValue;
            }
            return await extractFunc();
        }

        private async Task<int> GetIntValueWithRenewOverrideAsync(string? frontendValue, Func<Task<int>> extractFunc)
        {
            if (!string.IsNullOrEmpty(frontendValue) && int.TryParse(frontendValue, out var parsedValue))
            {
                _logger.LogDebug("Usando override async int de frontend: {Value}", parsedValue);
                return parsedValue;
            }
            return await extractFunc();
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

    public class RenewalContext
    {
        public int ScanId { get; set; }
        public int ClienteId { get; set; }
        public int CompaniaId { get; set; }
        public int SeccionId { get; set; }
        public int UserId { get; set; }
    }

    public class RenewalContextInfo
    {
        public SegurosApp.API.DTOs.Velneo.Item.ClienteItem? Cliente { get; set; }
        public SegurosApp.API.DTOs.Velneo.Item.CompaniaItem? Compania { get; set; }
        public SegurosApp.API.DTOs.Velneo.Item.SeccionItem? Seccion { get; set; }
    }

    public class RenewalSpecificData
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

    public class PolizaAnteriorInfo
    {
        public string NumeroPoliza { get; set; } = "";
        public DateTime? FechaVencimiento { get; set; }
    }

    public class RenewalValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime? FechaVencimientoAnterior { get; set; }
        public object? PolizaAnterior { get; set; }

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
    }

    public class RenewalDateValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public DateTime? FechaVencimiento { get; set; }
    }

    #endregion
}