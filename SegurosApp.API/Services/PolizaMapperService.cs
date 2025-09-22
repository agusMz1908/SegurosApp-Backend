using Microsoft.EntityFrameworkCore;
using SegurosApp.API.Data;
using SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.SegurosApp.API.DTOs;
using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.DTOs.Velneo.Response;
using SegurosApp.API.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services
{
    public class PolizaMapperService
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly AppDbContext _context;
        private readonly ILogger<PolizaMapperService> _logger;
        private readonly CompanyMapperFactory _companyMapperFactory;

        public PolizaMapperService(
            IVelneoMasterDataService masterDataService,
            AppDbContext context,
            ILogger<PolizaMapperService> logger,
            CompanyMapperFactory companyMapperFactory)
        {
            _masterDataService = masterDataService;
            _context = context;
            _logger = logger;
            _companyMapperFactory = companyMapperFactory;
        }

        public async Task<PolizaMappingWithContextResponse> MapToPolizaWithContextAsync(
            Dictionary<string, object> extractedData,
            PreSelectionContext context)
        {
            _logger.LogInformation("Iniciando mapeo con contexto para scan {ScanId} - Cliente:{ClienteId}, Compañía:{CompaniaId}, Sección:{SeccionId}",
                context.ScanId, context.ClienteId, context.CompaniaId, context.SeccionId);

            var response = new PolizaMappingWithContextResponse();

            try
            {
                var normalizedData = await NormalizeExtractedDataAsync(extractedData, context.CompaniaId);

                var mappedData = await MapBasicPolizaDataAsync(normalizedData, context);
                response.MappedData = mappedData;
                response.NormalizedData = normalizedData; 

                var criticalValidation = ValidateCriticalFields(mappedData, normalizedData);

                var suggestions = await GenerateAutoSuggestionsAsync(normalizedData, mappedData);
                response.AutoSuggestions = suggestions;

                var requiresAttention = IdentifyFieldsRequiringAttention(mappedData, normalizedData);
                response.RequiresAttention = requiresAttention;

                var metrics = CalculateMappingMetrics(mappedData, normalizedData, criticalValidation);
                response.MappingMetrics = metrics;
                response.CompletionPercentage = metrics.OverallCompletionPercentage;

                response.IsComplete = DetermineIfComplete(mappedData, criticalValidation, requiresAttention);
                response.OverallCompletionPercentage = response.CompletionPercentage;
                response.ConfirmedByPreSelection = new List<string>
        {
            "cliente", "compania", "seccion"
        };

                _logger.LogInformation("Mapeo con contexto completado: {CompletionPercentage:F1}% - Listo: {IsComplete}",
                    response.CompletionPercentage, response.IsComplete);

                _logger.LogInformation("Datos normalizados incluidos en respuesta: {CamposNormalizados} campos",
                    normalizedData.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en mapeo con contexto para scan {ScanId}", context.ScanId);

                return new PolizaMappingWithContextResponse
                {
                    IsComplete = false,
                    CompletionPercentage = 0,
                    NormalizedData = new Dictionary<string, object>(), 
                    RequiresAttention = new List<FieldMappingIssue>
            {
                new FieldMappingIssue
                {
                    FieldName = "general",
                    IssueType = "ProcessingError",
                    Description = $"Error procesando mapeo: {ex.Message}",
                    Severity = "Error"
                }
            }
                };
            }
        }

        public async Task<VelneoPolizaRequest> CreateVelneoRequestFromScanAsync(
            int scanId,
            int userId,
            CreatePolizaVelneoRequest? overrides = null)
        {
            _logger.LogInformation("Creando request Velneo para scan {ScanId}, usuario {UserId}", scanId, userId);

            var scan = await _context.DocumentScans
                .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId);

            if (scan == null)
            {
                _logger.LogError("Scan {ScanId} no encontrado para usuario {UserId}", scanId, userId);
                throw new ArgumentException($"Scan {scanId} no encontrado para usuario {userId}");
            }

            var extractedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(scan.ExtractedData)
                ?? new Dictionary<string, object>();
            var normalizedData = await NormalizeExtractedDataAsync(extractedData, scan.CompaniaId);

            var contextClienteId = GetValueWithOverride(overrides?.ClienteId, scan.ClienteId, "ClienteId");
            var contextCompaniaId = GetValueWithOverride(overrides?.CompaniaId, scan.CompaniaId, "CompaniaId");
            var contextSeccionId = GetValueWithOverride(overrides?.SeccionId, scan.SeccionId, "SeccionId");

            ClienteItem? clienteInfo = null;
            CompaniaItem? companiaInfo = null;
            SeccionItem? seccionInfo = null;

            try
            {
                clienteInfo = await _masterDataService.GetClienteDetalleAsync(contextClienteId);

                var companias = await _masterDataService.GetCompaniasAsync();
                companiaInfo = companias.FirstOrDefault(c => c.id == contextCompaniaId);

                var secciones = await _masterDataService.GetSeccionesAsync(contextCompaniaId);
                seccionInfo = secciones.FirstOrDefault(s => s.id == contextSeccionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error obteniendo información de maestros: {Error}", ex.Message);
            }

            var rawStartDate = ExtractStartDate(normalizedData);
            var rawEndDate = ExtractEndDate(normalizedData);
            var formattedStartDate = ConvertToVelneoDateFormat(rawStartDate);
            var formattedEndDate = ConvertToVelneoDateFormat(rawEndDate);
            var extractedPremium = ExtractPremium(normalizedData);
            var extractedTotal = ExtractTotalAmount(normalizedData);
            var extractedCuotas = ExtractInstallmentCount(normalizedData);
            var extractedPaymentMethod = ExtractPaymentMethod(normalizedData);

            var request = new VelneoPolizaRequest
            {
                clinro = contextClienteId,
                comcod = contextCompaniaId,
                seccod = contextSeccionId,
                conpol = ExtractPolicyNumber(normalizedData),
                conend = ExtractEndorsement(normalizedData),
                confchdes = GetStringValueWithOverride(overrides?.StartDateOverride, formattedStartDate, "FechaInicio"),
                confchhas = GetStringValueWithOverride(overrides?.EndDateOverride, formattedEndDate, "FechaFin"),
                conpremio = (int)Math.Round(extractedPremium),
                contot = (int)Math.Round(extractedTotal),
                conmaraut = GetStringValueWithOverride(overrides?.VehicleBrandOverride, ExtractVehicleBrand(normalizedData), "MarcaVehiculo"),
                conmodaut = GetStringValueWithOverride(overrides?.VehicleModelOverride, ExtractVehicleModel(normalizedData), "ModeloVehiculo"),
                conanioaut = overrides?.VehicleYearOverride ?? ExtractVehicleYear(normalizedData),
                conmotor = GetStringValueWithOverride(overrides?.MotorNumberOverride, ExtractMotorNumber(normalizedData), "NumeroMotor"),
                conchasis = GetStringValueWithOverride(overrides?.ChassisNumberOverride, ExtractChassisNumber(normalizedData), "NumeroChasis"),
                conmataut = ExtractVehiclePlate(normalizedData),
                clinom = clienteInfo?.clinom ?? "",
                condom = clienteInfo?.clidir ?? ExtractClientAddress(normalizedData),
                clinro1 = ExtractBeneficiaryId(normalizedData),
                dptnom = overrides?.DepartmentIdOverride ?? await FindDepartmentIdAsync(normalizedData),
                combustibles = overrides?.FuelCodeOverride ?? await FindFuelCodeAsync(normalizedData),
                desdsc = overrides?.DestinationIdOverride ?? await FindDestinationIdAsync(normalizedData),
                catdsc = overrides?.CategoryIdOverride ?? await FindCategoryIdAsync(normalizedData),
                caldsc = overrides?.QualityIdOverride ?? await FindQualityIdAsync(normalizedData),
                tarcod = overrides?.TariffIdOverride ?? await FindTariffIdAsync(normalizedData),
                corrnom = overrides?.BrokerIdOverride ?? ExtractCorredorId(normalizedData),
                consta = MapPaymentMethodCode(extractedPaymentMethod),
                concuo = extractedCuotas,
                moncod = overrides?.CurrencyIdOverride ?? ExtractCurrencyCode(normalizedData),
                conviamon = overrides?.PaymentCurrencyIdOverride ?? ExtractCurrencyCode(normalizedData),
                congesti = "1",
                congeses = "1",
                contra = "1",
                convig = "1",
                com_alias = companiaInfo?.comnom ?? "",
                ramo = seccionInfo?.seccion ?? "",
                ingresado = DateTime.UtcNow,
                last_update = DateTime.UtcNow,
                app_id = scanId,
                observaciones = "",
                conpadre = 0
            };
            request.observaciones = FormatObservations(overrides?.Notes, overrides?.UserComments, request, normalizedData);

            await ValidateVelneoRequest(request);
            return request;
        }

        public async Task<VelneoPolizaRequest> CreateVelneoRequestFromRenewAsync(
            int scanId,
            int userId,
            RenewPolizaRequest renewRequest,
            object? polizaAnterior = null)
        {
            _logger.LogInformation("Creando request Velneo para renovación - Scan: {ScanId}, Usuario: {UserId}, PolizaAnterior: {PolizaAnteriorId}",
                scanId, userId, renewRequest.PolizaAnteriorId);

            var scan = await _context.DocumentScans
                .FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId);

            if (scan == null)
            {
                _logger.LogError("Scan {ScanId} no encontrado para usuario {UserId}", scanId, userId);
                throw new ArgumentException($"Scan {scanId} no encontrado para usuario {userId}");
            }

            var extractedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(scan.ExtractedData)
                ?? new Dictionary<string, object>();
            var normalizedData = await NormalizeExtractedDataAsync(extractedData, scan.CompaniaId);

            // ✅ USAR CONTEXTO DEL SCAN (heredado de la selección de póliza)
            var contextClienteId = scan.ClienteId ?? throw new ArgumentException("ClienteId requerido para renovación");
            var contextCompaniaId = scan.CompaniaId ?? throw new ArgumentException("CompaniaId requerido para renovación");
            var contextSeccionId = scan.SeccionId ?? throw new ArgumentException("SeccionId requerido para renovación");

            // ✅ OBTENER INFORMACIÓN DEL CONTEXTO (igual que el método original)
            ClienteItem? clienteInfo = null;
            CompaniaItem? companiaInfo = null;
            SeccionItem? seccionInfo = null;

            try
            {
                clienteInfo = await _masterDataService.GetClienteDetalleAsync(contextClienteId);

                var companias = await _masterDataService.GetCompaniasAsync();
                companiaInfo = companias.FirstOrDefault(c => c.id == contextCompaniaId);

                var secciones = await _masterDataService.GetSeccionesAsync(contextCompaniaId);
                seccionInfo = secciones.FirstOrDefault(s => s.id == contextSeccionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error obteniendo información de maestros para renovación: {Error}", ex.Message);
            }

            // ✅ PROCESAR FECHAS (igual que el método original)
            var rawStartDate = GetStringValueWithRenewOverride(renewRequest.FechaDesde, ExtractStartDate(normalizedData), "FechaInicio");
            var rawEndDate = GetStringValueWithRenewOverride(renewRequest.FechaHasta, ExtractEndDate(normalizedData), "FechaFin");
            var formattedStartDate = ConvertToVelneoDateFormat(rawStartDate);
            var formattedEndDate = ConvertToVelneoDateFormat(rawEndDate);

            // ✅ PROCESAR MONTOS
            var extractedPremium = renewRequest.Premio ?? ExtractPremium(normalizedData);
            var extractedTotal = renewRequest.MontoTotal ?? ExtractTotalAmount(normalizedData);
            var extractedCuotas = renewRequest.CantidadCuotas ?? ExtractInstallmentCount(normalizedData);
            var extractedPaymentMethod = ExtractPaymentMethod(normalizedData);

            // ✅ CREAR REQUEST CON LA MISMA ESTRUCTURA QUE EL MÉTODO ORIGINAL
            var request = new VelneoPolizaRequest
            {
                // IDs principales (del contexto del scan)
                clinro = contextClienteId,
                comcod = contextCompaniaId,
                seccod = contextSeccionId,

                // Datos de póliza con overrides del frontend
                conpol = renewRequest.NumeroPoliza ?? ExtractPolicyNumber(normalizedData),
                conend = ExtractEndorsement(normalizedData),
                confchdes = formattedStartDate,
                confchhas = formattedEndDate,
                conpremio = (int)Math.Round(extractedPremium),
                contot = (int)Math.Round(extractedTotal),

                // Datos del vehículo con overrides del frontend
                conmaraut = renewRequest.VehiculoMarca ?? ExtractVehicleBrand(normalizedData),
                conmodaut = renewRequest.VehiculoModelo ?? ExtractVehicleModel(normalizedData),
                conanioaut = renewRequest.VehiculoAno ?? ExtractVehicleYear(normalizedData),
                conmotor = renewRequest.VehiculoMotor ?? ExtractMotorNumber(normalizedData),
                conchasis = renewRequest.VehiculoChasis ?? ExtractChassisNumber(normalizedData),
                conmataut = renewRequest.VehiculoPatente ?? ExtractVehiclePlate(normalizedData),

                // Datos del cliente
                clinom = clienteInfo?.clinom ?? "",
                condom = clienteInfo?.clidir ?? ExtractClientAddress(normalizedData),
                clinro1 = ExtractBeneficiaryId(normalizedData),

                // ✅ MASTER DATA CON OVERRIDES DEL FRONTEND (PRIORITARIOS)
                dptnom = await GetIntValueWithRenewOverrideAsync(renewRequest.DepartamentoId, () => FindDepartmentIdAsync(normalizedData)),
                combustibles = await GetStringValueWithRenewOverrideAsync(renewRequest.CombustibleId, () => FindFuelCodeAsync(normalizedData)),
                desdsc = await GetIntValueWithRenewOverrideAsync(renewRequest.DestinoId, () => FindDestinationIdAsync(normalizedData)),
                catdsc = await GetIntValueWithRenewOverrideAsync(renewRequest.CategoriaId, () => FindCategoryIdAsync(normalizedData)),
                caldsc = await GetIntValueWithRenewOverrideAsync(renewRequest.CalidadId, () => FindQualityIdAsync(normalizedData)),
                tarcod = await GetIntValueWithRenewOverrideAsync(renewRequest.TarifaId, () => FindTariffIdAsync(normalizedData)),
                corrnom = GetIntValueWithRenewOverride(renewRequest.CorredorId, ExtractCorredorId(normalizedData)),

                // Condiciones de pago
                consta = MapPaymentMethodCode(extractedPaymentMethod),
                concuo = extractedCuotas,
                moncod = GetIntValueWithRenewOverride(renewRequest.MonedaId, ExtractCurrencyCode(normalizedData)),
                conviamon = GetIntValueWithRenewOverride(renewRequest.MonedaId, ExtractCurrencyCode(normalizedData)),

                // Estados (igual que el método original)
                congesti = "1",
                congeses = "1",
                contra = "1",
                convig = "1",

                // Datos adicionales
                com_alias = companiaInfo?.comnom ?? "",
                ramo = seccionInfo?.seccion ?? "",

                // Metadatos
                ingresado = DateTime.UtcNow,
                last_update = DateTime.UtcNow,
                app_id = scanId,
                conpadre = 0
            };

            var polizaAnteriorNumero = ExtractPolizaNumber(polizaAnterior);
            request.observaciones = FormatRenovationObservations(renewRequest, request, normalizedData, polizaAnteriorNumero);

            // ✅ LOG DE OVERRIDES APLICADOS
            LogAppliedOverrides(renewRequest);

            await ValidateVelneoRequest(request);
            return request;
        }

        private string ExtractVehiclePlate(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.matricula", "matricula", "MATRICULA", "placa", "patente"
            };

            var value = GetFirstValidValue(data, possibleFields);

            if (string.Equals(value, "PATENTE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "MATRICULA", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(value))
            {
                var realPatente = FindRealPatenteInData(data);
                if (!string.IsNullOrEmpty(realPatente))
                {
                    _logger.LogInformation("✅ Patente encontrada en datos: {Patente}", realPatente);
                    return realPatente;
                }
            }

            return CleanPatenteValue(value);
        }

        private string ExtractClientAddress(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "cliente.direccion", "direccion", "DIRECCION", "domicilio", "address"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private int ExtractBeneficiaryId(Dictionary<string, object> data)
        {
            return 0;
        }

        private int ExtractCorredorId(Dictionary<string, object> data)
        {
            return 0;
        }

        private int ExtractCurrencyCode(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "moneda", "currency", "divisa" };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var normalized = value.ToUpper();

                    if (normalized.Contains("USD") || normalized.Contains("DOLAR") || normalized.Contains("DOL"))
                        return 2; 

                    if (normalized.Contains("UYU") || normalized.Contains("PESO") || normalized.Contains("PES"))
                        return 1; 

                    if (normalized.Contains("EUR") || normalized.Contains("EURO") || normalized.Contains("EU"))
                        return 4; 

                    if (normalized.Contains("REAL") || normalized.Contains("BRL") || normalized.Contains("RS"))
                        return 3; 

                    if (normalized.Contains("UF") || normalized.Contains("UNIDAD"))
                        return 5; 
                }
            }

            return 1;
        }

        private int GetValueWithOverride(int? overrideValue, int? dbValue, string fieldName)
        {
            if (overrideValue.HasValue && overrideValue.Value > 0)
            {
                _logger.LogDebug("Usando override para {FieldName}: {Value}", fieldName, overrideValue.Value);
                return overrideValue.Value;
            }

            var result = dbValue ?? 0;
            _logger.LogDebug("Usando BD para {FieldName}: {Value}", fieldName, result);
            return result;
        }

        private string FormatObservations(string? notes, string? userComments, VelneoPolizaRequest request, Dictionary<string, object> normalizedData)
        {
            var parts = new List<string> { "Generado desde escaneo automático." };
            parts.Add("");

            if (!string.IsNullOrWhiteSpace(notes))
                parts.Add($"Notas: {notes}");

            if (!string.IsNullOrWhiteSpace(userComments))
                parts.Add($"Comentarios: {userComments}");

            if (request.concuo > 1 && request.contot > 0)
            {
                var cronograma = GenerarCronogramaCuotasFromData(request, normalizedData);
                parts.Add(cronograma);
            }

            return string.Join("\n", parts);
        }

        private string GenerarCronogramaCuotasFromData(VelneoPolizaRequest request, Dictionary<string, object> normalizedData)
        {
            try
            {
                var cronograma = new StringBuilder();

                cronograma.AppendLine("CRONOGRAMA DE CUOTAS");
                cronograma.AppendLine($"Total: ${request.contot:N2} en {request.concuo} cuotas");
                cronograma.AppendLine(); 

                bool usedRealData = false;

                for (int i = 0; i < request.concuo; i++)
                {
                    var fechaKey = $"pago.cuotas[{i}].vencimiento";
                    var montoKey = $"pago.cuotas[{i}].prima";

                    if (normalizedData.ContainsKey(fechaKey) && normalizedData.ContainsKey(montoKey))
                    {
                        var fechaRaw = normalizedData[fechaKey].ToString();
                        var montoRaw = normalizedData[montoKey].ToString();

                        var fechaMatch = Regex.Match(fechaRaw, @"(\d{2}[-/]\d{2}[-/]\d{4})");
                        var fecha = fechaMatch.Success ? fechaMatch.Groups[1].Value : "Fecha no disponible";

                        var montoMatch = Regex.Match(montoRaw, @"([\d.,]+)");
                        var monto = montoMatch.Success ? montoMatch.Groups[1].Value : "0";

                        cronograma.AppendLine($"Cuota {i + 1:D2}: {fecha} - $ {monto}");
                        usedRealData = true;
                    }
                }

                if (!usedRealData)
                {
                    var fechaInicio = DateTime.TryParse(request.confchdes, out var startDate) ? startDate : DateTime.Now;
                    var montoCuota = Math.Round((decimal)request.contot / request.concuo, 2);

                    for (int i = 1; i <= request.concuo; i++)
                    {
                        var fechaVencimiento = fechaInicio.AddDays(30 * i);
                        var monto = i == request.concuo ? request.contot - (montoCuota * (request.concuo - 1)) : montoCuota;
                        cronograma.AppendLine($"Cuota {i:D2}: {fechaVencimiento:dd/MM/yyyy} - ${monto:N2}");
                    }
                }

                cronograma.AppendLine("FIN CRONOGRAMA CUOTAS");

                return cronograma.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error generando cronograma de cuotas: {Error}", ex.Message);
                return "\nError generando cronograma de cuotas.";
            }
        }

        private string GetStringValueWithOverride(string? overrideValue, string defaultValue, string fieldName)
        {
            if (!string.IsNullOrEmpty(overrideValue) &&
                overrideValue != "string" &&
                overrideValue.Trim() != "string")
            {
                _logger.LogDebug("Usando override para {FieldName}: '{Value}'", fieldName, overrideValue);
                return overrideValue.Trim();
            }

            _logger.LogDebug("Usando valor por defecto para {FieldName}: '{Value}'", fieldName, defaultValue);
            return defaultValue;
        }

        #region Normalización de Campos por Compañía - MEJORADA

        private async Task<Dictionary<string, object>> NormalizeExtractedDataAsync(Dictionary<string, object> extractedData, int? companiaId = null)
        {
            var normalized = new Dictionary<string, object>(extractedData);

            // ✅ NUEVO: Usar mappers específicos si está habilitado
            if (companiaId.HasValue)
            {
                try
                {
                    var mapper = _companyMapperFactory.GetMapper(companiaId);
                    var enhancedNormalized = await mapper.NormalizeFieldsAsync(normalized, _masterDataService);

                    _logger.LogInformation("Usando normalización mejorada para compañía {CompaniaId}: {Mapper}",
                        companiaId, mapper.GetCompanyName());

                    return enhancedNormalized;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error en normalización mejorada, usando método legacy");
                    // Fallback al método actual
                }
            }

            // 🔄 LEGACY: Método actual como fallback
            if (companiaId.HasValue)
            {
                switch (companiaId.Value)
                {
                    case 3:
                        _logger.LogInformation("Normalizando campos de MAPFRE (companiaId=3) a formato BSE");
                        NormalizeMapfreFields(normalized);
                        break;
                    case 4:
                        _logger.LogInformation("Normalizando campos de SURA (companiaId=4) a formato BSE");
                        NormalizeSuraFields(normalized);
                        break;
                }
            }

            return normalized;
        }

        private void NormalizeMapfreFields(Dictionary<string, object> data)
        {
            _logger.LogDebug("Iniciando normalización de campos MAPFRE");
            NormalizeMapfreCuotas(data);
            NormalizeMapfrePatente(data);
            NormalizeMapfreMontos(data);

            for (int i = 1; i <= 12; i++)
            {
                var bseIndex = i - 1;

                if (data.ContainsKey($"pago.vencimiento_cuota[{i}]"))
                {
                    var fecha = data[$"pago.vencimiento_cuota[{i}]"].ToString();
                    data[$"pago.cuotas[{bseIndex}].vencimiento"] = $"Vencimiento:\n{fecha}";
                }

                if (data.ContainsKey($"pago.cuota_monto[{i}]"))
                {
                    var monto = data[$"pago.cuota_monto[{i}]"].ToString();
                    data[$"pago.cuotas[{bseIndex}].prima"] = $"Prima:\n$ {monto}";
                }
            }

            if (data.ContainsKey("costo.premio_total") && !data.ContainsKey("financiero.premio_total"))
            {
                data["financiero.premio_total"] = data["costo.premio_total"];
            }

            if (data.ContainsKey("costo.costo") && !data.ContainsKey("poliza.prima_comercial"))
            {
                data["poliza.prima_comercial"] = data["costo.costo"];
            }
        }

        private void NormalizeMapfreCuotas(Dictionary<string, object> data)
        {
            var possibleCuotasFields = new[] {
                "pago.modo_facturacion", "cantidadCuotas", "cantidad_cuotas", "cuotas", "concuo"
            };

            int realCuotas = 1;
            foreach (var field in possibleCuotasFields)
            {
                if (data.ContainsKey(field))
                {
                    var value = data[field].ToString();
                    var match = Regex.Match(value, @"(\d+)\s*cuotas?", RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var cuotas) && cuotas > 0)
                    {
                        realCuotas = cuotas;
                        _logger.LogInformation("🎯 MAPFRE - Cuotas encontradas: {Cuotas}", realCuotas);
                        break;
                    }
                }
            }

            int cuotasContadas = 0;
            for (int i = 1; i <= 12; i++)
            {
                if (data.ContainsKey($"pago.cuota_monto[{i}]"))
                {
                    var monto = data[$"pago.cuota_monto[{i}]"].ToString();
                    if (!string.IsNullOrEmpty(monto) && monto != "0" && !monto.Contains("$0"))
                    {
                        cuotasContadas++;
                    }
                }
            }

            if (cuotasContadas > 0)
            {
                realCuotas = Math.Max(realCuotas, cuotasContadas);
            }

            data["pago.cantidad_cuotas"] = realCuotas.ToString();
            data["cantidadCuotas"] = realCuotas;

            _logger.LogInformation("✅ MAPFRE - Cuotas normalizadas: {Cuotas}", realCuotas);
        }

        private void NormalizeMapfrePatente(Dictionary<string, object> data)
        {
            var patenteFields = new[] { "vehiculo.matricula", "matricula", "MATRICULA", "placa", "patente" };

            foreach (var field in patenteFields)
            {
                if (data.ContainsKey(field))
                {
                    var value = data[field].ToString();

                    if (string.Equals(value, "PATENTE", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("MAPFRE - Campo {Field} contiene solo 'PATENTE', buscando valor real", field);

                        var realPatente = FindRealPatenteInData(data);
                        if (!string.IsNullOrEmpty(realPatente))
                        {
                            data[field] = realPatente;
                            _logger.LogInformation("MAPFRE - Patente corregida: {Patente}", realPatente);
                        }
                        else
                        {
                            data[field] = ""; 
                            _logger.LogWarning("⚠MAPFRE - No se encontró patente real, limpiando campo");
                        }
                    }
                    else
                    {
                        var cleanPatente = CleanPatenteValue(value);
                        if (cleanPatente != value)
                        {
                            data[field] = cleanPatente;
                            _logger.LogInformation("MAPFRE - Patente limpiada: '{Original}' -> '{Clean}'", value, cleanPatente);
                        }
                    }
                }
            }
        }

        private string FindRealPatenteInData(Dictionary<string, object> data)
        {
            var uruguayanPlatePatterns = new[]
            {
                @"\b[A-Z]{3}\s*\d{4}\b",         
                @"\b[A-Z]{2}\s*\d{4,6}\b",        
                @"\b[A-Z]{3}-\d{4}\b",             
                @"\b[A-Z]{2}-\d{4,6}\b",           
                @"\b[A-Z]{1,3}\s*\d{3,6}\b"        
            };

            _logger.LogInformation("Buscando patente real en {CampoCount} campos de datos", data.Count);

            foreach (var kvp in data)
            {
                if (kvp.Key.ToLower().Contains("matricula") ||
                    kvp.Key.ToLower().Contains("patente") ||
                    kvp.Key.ToLower().Contains("placa"))
                {
                    continue; 
                }

                var value = kvp.Value?.ToString() ?? "";
                if (value.Length < 6 || value.Length > 50) continue; 

                foreach (var pattern in uruguayanPlatePatterns)
                {
                    var matches = Regex.Matches(value, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        var candidate = match.Value.Replace(" ", "").Replace("-", "").ToUpper();
                        if (IsValidPatente(candidate))
                        {
                            _logger.LogInformation("Patente encontrada en campo '{Campo}': '{Patente}' (valor original: '{Valor}')",
                                kvp.Key, candidate, value);
                            return candidate;
                        }
                    }
                }
            }

            foreach (var kvp in data)
            {
                var value = kvp.Value?.ToString() ?? "";
                if (value.Length < 6 || value.Length > 200) continue;

                foreach (var pattern in uruguayanPlatePatterns)
                {
                    var matches = Regex.Matches(value, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        var candidate = match.Value.Replace(" ", "").Replace("-", "").ToUpper();
                        if (IsValidPatente(candidate))
                        {
                            _logger.LogInformation("Patente encontrada (segunda pasada) en campo '{Campo}': '{Patente}'",
                                kvp.Key, candidate);
                            return candidate;
                        }
                    }
                }
            }

            foreach (var kvp in data)
            {
                var value = kvp.Value?.ToString() ?? "";
                var aggressiveMatches = Regex.Matches(value, @"[A-Z]{2,3}\s*\d{3,6}", RegexOptions.IgnoreCase);

                foreach (Match match in aggressiveMatches)
                {
                    var candidate = match.Value.Replace(" ", "").ToUpper();
                    if (IsValidPatente(candidate))
                    {
                        _logger.LogInformation("Patente encontrada (búsqueda agresiva) en campo '{Campo}': '{Patente}'",
                            kvp.Key, candidate);
                        return candidate;
                    }
                }
            }

            _logger.LogWarning("No se encontró ninguna patente válida en los datos");
            return "";
        }

        private bool IsValidPatente(string patente)
        {
            if (string.IsNullOrEmpty(patente)) return false;

            var invalidValues = new[] { "PATENTE", "MATRICULA", "PLACA", "VEHICULO", "AUTO", "COCHE" };
            if (invalidValues.Contains(patente.ToUpper())) return false;

            var letterCount = patente.Count(char.IsLetter);
            var digitCount = patente.Count(char.IsDigit);

            if (letterCount >= 2 && digitCount >= 3 && patente.Length >= 5 && patente.Length <= 8)
            {
                return true;
            }

            return false;
        }

        private string CleanPatenteValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            _logger.LogDebug("Limpiando valor de patente: '{Value}'", value);
            var cleaned = value.Replace("\n", " ").Replace("\r", " ").Trim();

            var prefixesToRemove = new[] {
                "MATRÍCULA:", "MATRICULA:", "PATENTE:", "PLACA:",
                "MATRÍCULA", "MATRICULA", "PATENTE", "PLACA"
            };

            foreach (var prefix in prefixesToRemove)
            {
                if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(prefix.Length).Trim();
                    _logger.LogDebug("Removido prefijo '{Prefix}', resultado: '{Cleaned}'", prefix, cleaned);
                    break;
                }
            }

            var genericWords = new[] { "PATENTE", "MATRICULA", "MATRÍCULA", "PLACA", "VEHICULO", "AUTO", "COCHE" };
            if (genericWords.Any(word => string.Equals(cleaned, word, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Valor limpiado es palabra genérica: '{Cleaned}', devolviendo vacío", cleaned);
                return "";
            }

            var patentePatterns = new[]
            {
                @"[A-Z]{3}\s*\d{4}",     
                @"[A-Z]{2}\s*\d{4,6}",   
                @"[A-Z]{3}-\d{4}",      
                @"[A-Z]{2}-\d{4,6}"    
            };

            foreach (var pattern in patentePatterns)
            {
                var match = Regex.Match(cleaned, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var patente = match.Value.Replace(" ", "").Replace("-", "").ToUpper();
                    _logger.LogDebug("Patente encontrada con patrón '{Pattern}': '{Patente}'", pattern, patente);
                    return patente;
                }
            }

            if (IsValidPatente(cleaned))
            {
                var normalizedPatente = cleaned.Replace(" ", "").Replace("-", "").ToUpper();
                _logger.LogDebug("Patente válida después de limpieza: '{Patente}'", normalizedPatente);
                return normalizedPatente;
            }

            _logger.LogDebug("No se pudo extraer patente válida de: '{Value}'", value);
            return "";
        }

        private void NormalizeMapfreMontos(Dictionary<string, object> data)
        {
            if (data.ContainsKey("financiero.premio_total") && data.ContainsKey("cantidadCuotas"))
            {
                var totalAmount = ExtractAmountFromString(data["financiero.premio_total"].ToString());
                var cantidadCuotas = Convert.ToInt32(data["cantidadCuotas"]);

                if (totalAmount > 0 && cantidadCuotas > 1)
                {
                    var valorCuota = totalAmount / cantidadCuotas;
                    data["pago.valor_cuota"] = valorCuota.ToString("F2");
                    _logger.LogInformation("MAPFRE - Valor cuota calculado: ${ValorCuota} (Total: ${Total} / {Cuotas} cuotas)",
                        valorCuota, totalAmount, cantidadCuotas);
                }
            }
        }

        private void NormalizeSuraFields(Dictionary<string, object> data)
        {
            _logger.LogDebug("Iniciando normalización de campos SURA");
            NormalizeSuraCuotas(data);
            NormalizeSuraPatente(data);
            NormalizeSuraMontos(data);

            if (data.ContainsKey("datos_financiero") && !data.ContainsKey("financiero.premio_total"))
            {
                data["financiero.premio_total"] = data["datos_financiero"];
            }
        }

        private void NormalizeSuraCuotas(Dictionary<string, object> data)
        {
            var possibleCuotasFields = new[] {
                "pago.modo_facturacion", "cantidadCuotas", "cantidad_cuotas", "cuotas", "concuo",
                "forma_pago", "modalidad_pago"
            };

            int realCuotas = 1;
            foreach (var field in possibleCuotasFields)
            {
                if (data.ContainsKey(field))
                {
                    var value = data[field].ToString();
                    var match = Regex.Match(value, @"(\d+)\s*cuotas?", RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var cuotas) && cuotas > 0)
                    {
                        realCuotas = cuotas;
                        _logger.LogInformation("SURA - Cuotas encontradas: {Cuotas}", realCuotas);
                        break;
                    }
                }
            }

            data["pago.cantidad_cuotas"] = realCuotas.ToString();
            data["cantidadCuotas"] = realCuotas;
            _logger.LogInformation("SURA - Cuotas normalizadas: {Cuotas}", realCuotas);
        }

        private void NormalizeSuraPatente(Dictionary<string, object> data)
        {
            NormalizeMapfrePatente(data);
        }

        private void NormalizeSuraMontos(Dictionary<string, object> data)
        {
            NormalizeMapfreMontos(data);
        }

        private int ExtractInstallmentCount(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "pago.cantidad_cuotas",      
                "cantidadCuotas",            
                "pago.modo_facturacion",
                "cantidad_cuotas",
                "cuotas",
                "concuo"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    _logger.LogInformation("ExtractInstallmentCount - Campo encontrado: '{Field}' = '{Value}'", field, value);

                    if (field == "cantidadCuotas" || field == "pago.cantidad_cuotas")
                    {
                        if (int.TryParse(value, out var directCuotas) && directCuotas > 0)
                        {
                            _logger.LogInformation("ExtractInstallmentCount - Cuotas desde campo normalizado: {Count}", directCuotas);
                            return directCuotas;
                        }
                    }

                    var match = Regex.Match(value, @"(\d+)\s*cuotas?", RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var cuotas) && cuotas > 0)
                    {
                        _logger.LogInformation("ExtractInstallmentCount - Cuotas extraídas: {Count} desde campo '{Field}'", cuotas, field);
                        return cuotas;
                    }

                    var numberMatch = Regex.Match(value, @"(\d+)");
                    if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out var number) && number > 0 && number <= 60)
                    {
                        _logger.LogInformation("ExtractInstallmentCount - Número extraído: {Count} desde campo '{Field}'", number, field);
                        return number;
                    }
                }
            }

            _logger.LogWarning("ExtractInstallmentCount - No se encontró número de cuotas, usando valor por defecto: 1");
            return 1;
        }

        private decimal ExtractTotalAmount(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "financiero.premio_total",
                "datos_financiero",
                "premio_total",
                "contot",
                "total",
                "PREMIO TOTAL A PAGAR"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    _logger.LogInformation("ExtractTotalAmount - Campo encontrado: '{Field}' = '{Value}'", field, value);

                    if (field == "datos_financiero" && value.Contains("Premio Total a Pagar"))
                    {
                        var match = Regex.Match(value, @"Premio Total a Pagar:\s*\$?\s*([\d.,]+)");
                        if (match.Success)
                        {
                            var amount = ExtractAmountFromString(match.Groups[1].Value);
                            if (amount > 0)
                            {
                                _logger.LogInformation("ExtractTotalAmount - Total extraído: {Amount} desde datos_financiero", amount);
                                return amount;
                            }
                        }
                    }
                    else
                    {
                        var amount = ExtractAmountFromString(value);
                        if (amount > 0)
                        {
                            _logger.LogInformation("ExtractTotalAmount - Total extraído: {Amount} desde campo '{Field}'", amount, field);
                            return amount;
                        }
                    }
                }
            }

            _logger.LogWarning("ExtractTotalAmount - No se encontró valor total en ningún campo");
            return 0;
        }

        #endregion

        #region Métodos Auxiliares

        private string ConvertToVelneoDateFormat(string dateStr)
        {
            _logger.LogDebug("ConvertToVelneoDateFormat input: '{DateStr}'", dateStr);

            if (string.IsNullOrEmpty(dateStr))
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                _logger.LogWarning("Fecha vacía, usando hoy: {Today}", today);
                return today;
            }

            try
            {
                var cleanDate = dateStr.Trim();
                if (DateTime.TryParseExact(cleanDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var alreadyFormatted))
                {
                    _logger.LogDebug("Fecha ya en formato correcto: {Date}", cleanDate);
                    return cleanDate;
                }

                var formats = new[]
                {
                    "dd/MM/yyyy",
                    "MM/dd/yyyy",
                    "dd-MM-yyyy",
                    "yyyy/MM/dd",
                    "dd/MM/yy",
                    "MM/dd/yy",
                    "yyyyMMdd",
                    "dd.MM.yyyy",
                    "yyyy-M-d",
                    "d/M/yyyy"
                };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(cleanDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        var result = parsedDate.ToString("yyyy-MM-dd");
                        _logger.LogDebug("Fecha convertida de '{Format}': {Input} -> {Output}", format, cleanDate, result);
                        return result;
                    }
                }

                if (DateTime.TryParse(cleanDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var flexibleDate))
                {
                    var result = flexibleDate.ToString("yyyy-MM-dd");
                    _logger.LogDebug("Fecha parseada flexible: {Input} -> {Output}", cleanDate, result);
                    return result;
                }

                var numbers = System.Text.RegularExpressions.Regex.Matches(cleanDate, @"\d+");
                if (numbers.Count >= 3)
                {
                    var day = int.Parse(numbers[0].Value);
                    var month = int.Parse(numbers[1].Value);
                    var year = int.Parse(numbers[2].Value);

                    if (year < 100)
                    {
                        year += (year < 30) ? 2000 : 1900;
                    }

                    if (day > 12 && month <= 12)
                    {
                        (day, month) = (month, day);
                    }

                    if (month >= 1 && month <= 12 && day >= 1 && day <= 31)
                    {
                        var extractedDate = new DateTime(year, month, day);
                        var result = extractedDate.ToString("yyyy-MM-dd");
                        _logger.LogDebug("Fecha extraída por números: {Input} -> {Output}", cleanDate, result);
                        return result;
                    }
                }

                _logger.LogError("No se pudo parsear fecha: '{DateStr}'", dateStr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parseando fecha '{DateStr}'", dateStr);
            }

            var fallback = DateTime.Today.ToString("yyyy-MM-dd");
            _logger.LogWarning("Usando fecha fallback: {Fallback}", fallback);
            return fallback;
        }

        private async Task<PolizaDataMapped> MapBasicPolizaDataAsync(
            Dictionary<string, object> extractedData,
            PreSelectionContext context)
        {
            return new PolizaDataMapped
            {
                NumeroPoliza = ExtractPolicyNumber(extractedData),
                Endoso = ExtractEndorsement(extractedData),
                FechaDesde = ExtractStartDate(extractedData),
                FechaHasta = ExtractEndDate(extractedData),
                Premio = ExtractPremium(extractedData),
                MontoTotal = ExtractTotalAmount(extractedData),
                VehiculoMarca = ExtractVehicleBrand(extractedData),
                VehiculoModelo = ExtractVehicleModel(extractedData),
                VehiculoAño = ExtractVehicleYear(extractedData),
                VehiculoMotor = ExtractMotorNumber(extractedData),
                VehiculoChasis = ExtractChassisNumber(extractedData),
                VehiculoCombustible = ExtractFuelType(extractedData),
                VehiculoDestino = ExtractDestination(extractedData),
                VehiculoCategoria = ExtractCategory(extractedData),
                MedioPago = ExtractPaymentMethod(extractedData),
                CantidadCuotas = ExtractInstallmentCount(extractedData),
                TipoMovimiento = ExtractMovementType(extractedData),
                AseguradoNombre = "[Confirmado por selección]",
                AseguradoDocumento = "[Confirmado por selección]"
            };
        }

        #endregion

        #region Validación de Campos Críticos

        private CriticalFieldsStatus ValidateCriticalFields(
            PolizaDataMapped mappedData,
            Dictionary<string, object> extractedData)
        {
            var status = new CriticalFieldsStatus();

            if (!string.IsNullOrEmpty(mappedData.NumeroPoliza) && mappedData.NumeroPoliza.Length >= 7)
            {
                status.HasPolicyNumber = true;
                status.FoundCritical.Add("Número de Póliza");
            }
            else
            {
                status.MissingCritical.Add("Número de Póliza");
            }

            if (!string.IsNullOrEmpty(mappedData.VehiculoMarca) || !string.IsNullOrEmpty(mappedData.VehiculoModelo))
            {
                status.HasVehicleInfo = true;
                status.FoundCritical.Add("Información del Vehículo");
            }
            else
            {
                status.MissingCritical.Add("Información del Vehículo");
            }

            if (!string.IsNullOrEmpty(mappedData.FechaDesde) && !string.IsNullOrEmpty(mappedData.FechaHasta))
            {
                status.HasDateRange = true;
                status.FoundCritical.Add("Rango de Fechas");
            }
            else
            {
                status.MissingCritical.Add("Rango de Fechas");
            }

            if (mappedData.Premio > 0 || mappedData.MontoTotal > 0)
            {
                status.HasPremiumInfo = true;
                status.FoundCritical.Add("Información de Premio");
            }
            else
            {
                status.MissingCritical.Add("Información de Premio");
            }

            return status;
        }

        #endregion

        #region Sugerencias Automáticas

        private async Task<List<FieldSuggestion>> GenerateAutoSuggestionsAsync(
            Dictionary<string, object> extractedData,
            PolizaDataMapped mappedData)
        {
            var suggestions = new List<FieldSuggestion>();

            try
            {
                if (!string.IsNullOrEmpty(mappedData.VehiculoCombustible))
                {
                    var fuelSuggestion = await _masterDataService.SuggestMappingAsync("combustible", mappedData.VehiculoCombustible);
                    if (fuelSuggestion.Confidence >= 0.7)
                    {
                        suggestions.Add(new FieldSuggestion
                        {
                            FieldName = "vehiculo.combustible",
                            DisplayName = "Combustible",
                            ScannedValue = mappedData.VehiculoCombustible,
                            SuggestedValue = fuelSuggestion.SuggestedValue!,
                            SuggestedLabel = fuelSuggestion.SuggestedLabel!,
                            Confidence = fuelSuggestion.Confidence,
                            Source = "AutoMapping"
                        });
                    }
                }

                if (!string.IsNullOrEmpty(mappedData.VehiculoDestino))
                {
                    var destinoSuggestion = await _masterDataService.SuggestMappingAsync("destino", mappedData.VehiculoDestino);
                    if (destinoSuggestion.Confidence >= 0.7)
                    {
                        suggestions.Add(new FieldSuggestion
                        {
                            FieldName = "vehiculo.destino",
                            DisplayName = "Destino del Vehículo",
                            ScannedValue = mappedData.VehiculoDestino,
                            SuggestedValue = destinoSuggestion.SuggestedValue!,
                            SuggestedLabel = destinoSuggestion.SuggestedLabel!,
                            Confidence = destinoSuggestion.Confidence,
                            Source = "AutoMapping"
                        });
                    }
                }

                if (!string.IsNullOrEmpty(mappedData.VehiculoCategoria))
                {
                    var categoriaSuggestion = await _masterDataService.SuggestMappingAsync("categoria", mappedData.VehiculoCategoria);
                    if (categoriaSuggestion.Confidence >= 0.7)
                    {
                        suggestions.Add(new FieldSuggestion
                        {
                            FieldName = "vehiculo.categoria",
                            DisplayName = "Categoría del Vehículo",
                            ScannedValue = mappedData.VehiculoCategoria,
                            SuggestedValue = categoriaSuggestion.SuggestedValue!,
                            SuggestedLabel = categoriaSuggestion.SuggestedLabel!,
                            Confidence = categoriaSuggestion.Confidence,
                            Source = "AutoMapping"
                        });
                    }
                }

                _logger.LogInformation("Generadas {Count} sugerencias automáticas", suggestions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando sugerencias automáticas");
            }

            return suggestions;
        }

        #endregion

        #region Campos que Requieren Atención

        private List<FieldMappingIssue> IdentifyFieldsRequiringAttention(
            PolizaDataMapped mappedData,
            Dictionary<string, object> extractedData)
        {
            var issues = new List<FieldMappingIssue>();

            if (string.IsNullOrEmpty(mappedData.NumeroPoliza))
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "numeroPoliza",
                    DisplayName = "Número de Póliza",
                    ScannedValue = GetValue(extractedData, "poliza.numero", "datos_poliza"),
                    IssueType = "MissingCritical",
                    Description = "No se pudo extraer el número de póliza del documento",
                    Severity = "Error",
                    IsRequired = true
                });
            }
            else if (mappedData.NumeroPoliza.Length < 7)
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "numeroPoliza",
                    DisplayName = "Número de Póliza",
                    ScannedValue = mappedData.NumeroPoliza,
                    IssueType = "InvalidFormat",
                    Description = "El número de póliza parece incompleto (menos de 7 dígitos)",
                    Severity = "Warning",
                    IsRequired = true
                });
            }

            if (string.IsNullOrEmpty(mappedData.FechaDesde) || string.IsNullOrEmpty(mappedData.FechaHasta))
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "vigencia",
                    DisplayName = "Fechas de Vigencia",
                    ScannedValue = $"Desde: {mappedData.FechaDesde}, Hasta: {mappedData.FechaHasta}",
                    IssueType = "MissingCritical",
                    Description = "No se pudieron extraer las fechas de vigencia completamente",
                    Severity = "Error",
                    IsRequired = true
                });
            }

            if (string.IsNullOrEmpty(mappedData.VehiculoMarca) && string.IsNullOrEmpty(mappedData.VehiculoModelo))
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "vehiculo",
                    DisplayName = "Información del Vehículo",
                    ScannedValue = GetValue(extractedData, "vehiculo.marca", "vehiculo.modelo"),
                    IssueType = "MissingCritical",
                    Description = "No se pudo extraer información básica del vehículo (marca/modelo)",
                    Severity = "Warning",
                    IsRequired = false
                });
            }

            if (mappedData.Premio <= 0 && mappedData.MontoTotal <= 0)
            {
                issues.Add(new FieldMappingIssue
                {
                    FieldName = "premio",
                    DisplayName = "Información de Premio",
                    ScannedValue = GetValue(extractedData, "poliza.prima_comercial", "financiero.premio_total"),
                    IssueType = "MissingCritical",
                    Description = "No se pudo extraer información de premio/monto de la póliza",
                    Severity = "Warning",
                    IsRequired = false
                });
            }

            return issues;
        }

        #endregion

        #region Métricas de Mapeo

        private MappingMetrics CalculateMappingMetrics(
            PolizaDataMapped mappedData,
            Dictionary<string, object> extractedData,
            CriticalFieldsStatus criticalStatus)
        {
            var policyFields = CategorizePolicyFields(mappedData, extractedData);
            var vehicleFields = CategorizeVehicleFields(mappedData, extractedData);
            var financialFields = CategorizeFinancialFields(mappedData, extractedData);
            var clientFields = CategorizeClientFields(mappedData, extractedData);
            var masterDataFields = CategorizeMasterDataFields(mappedData, extractedData);
            var optionalFields = CategorizeOptionalFields(mappedData, extractedData);
            var totalFieldsExpected = 20; 
            var mappedFields = CountMappedFields(mappedData);
            var overallCompletionPercentage = (decimal)mappedFields / totalFieldsExpected * 100;

            var performanceMetrics = new PerformanceMetrics
            {
                ProcessingTimeMs = 1000, 
                ValidationTimeMs = 200,
                MasterDataLookupTimeMs = 300,
                TotalMappingTimeMs = 1500,
                FieldsPerSecond = mappedFields > 0 ? (decimal)mappedFields / 1.5m : 0,
                AutoMappingSuccessRate = (decimal)mappedFields / extractedData.Count * 100,
                ManualReviewRequired = 0,
            };

            var confidenceBreakdown = CalculateConfidenceBreakdown(mappedData, extractedData);

            return new MappingMetrics
            {
                TotalFieldsScanned = extractedData.Count,
                FieldsMappedSuccessfully = mappedFields,
                FieldsWithIssues = 0, 
                FieldsRequireAttention = 0,
                OverallConfidence = criticalStatus.CriticalFieldsCompleteness,
                MappingQuality = DetermineMappingQuality(criticalStatus.CriticalFieldsCompleteness),
                MissingCriticalFields = criticalStatus.MissingCritical,
                OverallCompletionPercentage = overallCompletionPercentage,
                FieldsByCategory = new CategoryBreakdown
                {
                    PolicyFields = policyFields,
                    VehicleFields = vehicleFields,
                    FinancialFields = financialFields,
                    ClientFields = clientFields,
                    MasterDataFields = masterDataFields,
                    OptionalFields = optionalFields
                },

                Performance = performanceMetrics,
                Confidence = confidenceBreakdown,
                Suggestions = GenerateImprovementSuggestions(mappedData, extractedData)
            };
        }

        private CategoryMetric CategorizePolicyFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var policyFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
            {
                { "NumeroPoliza", (!string.IsNullOrEmpty(mappedData.NumeroPoliza), true) },
                { "Endoso", (!string.IsNullOrEmpty(mappedData.Endoso), false) },
                { "FechaDesde", (!string.IsNullOrEmpty(mappedData.FechaDesde), true) },
                { "FechaHasta", (!string.IsNullOrEmpty(mappedData.FechaHasta), true) },
                { "TipoMovimiento", (!string.IsNullOrEmpty(mappedData.TipoMovimiento), false) }
            };

            return CalculateCategoryMetric(policyFieldsMap, "Póliza");
        }
        private CategoryMetric CategorizeVehicleFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var vehicleFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
            {
                { "VehiculoMarca", (!string.IsNullOrEmpty(mappedData.VehiculoMarca), true) },
                { "VehiculoModelo", (!string.IsNullOrEmpty(mappedData.VehiculoModelo), true) },
                { "VehiculoAño", (mappedData.VehiculoAño > 0, true) },
                { "VehiculoMotor", (!string.IsNullOrEmpty(mappedData.VehiculoMotor), false) },
                { "VehiculoChasis", (!string.IsNullOrEmpty(mappedData.VehiculoChasis), false) },
                { "VehiculoCombustible", (!string.IsNullOrEmpty(mappedData.VehiculoCombustible), false) },
                { "VehiculoDestino", (!string.IsNullOrEmpty(mappedData.VehiculoDestino), false) },
                { "VehiculoCategoria", (!string.IsNullOrEmpty(mappedData.VehiculoCategoria), false) }
            };

            return CalculateCategoryMetric(vehicleFieldsMap, "Vehículo");
        }

        private CategoryMetric CategorizeFinancialFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var financialFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
            {
                { "Premio", (mappedData.Premio > 0, true) },
                { "MontoTotal", (mappedData.MontoTotal > 0, false) },
                { "MedioPago", (!string.IsNullOrEmpty(mappedData.MedioPago), false) },
                { "CantidadCuotas", (mappedData.CantidadCuotas > 0, false) }
            };

            return CalculateCategoryMetric(financialFieldsMap, "Financiero");
        }

        private CategoryMetric CategorizeClientFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var clientFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
            {
                { "AseguradoNombre", (!string.IsNullOrEmpty(mappedData.AseguradoNombre), true) },
                { "AseguradoDocumento", (!string.IsNullOrEmpty(mappedData.AseguradoDocumento), true) },
                { "AseguradoDepartamento", (!string.IsNullOrEmpty(mappedData.AseguradoDepartamento), false) },
                { "AseguradoDireccion", (!string.IsNullOrEmpty(mappedData.AseguradoDireccion), false) }
            };

            return CalculateCategoryMetric(clientFieldsMap, "Cliente");
        }

        private CategoryMetric CategorizeMasterDataFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var masterDataFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
            {
                { "Departamento", (extractedData.ContainsKey("asegurado.departamento"), false) },
                { "Combustible", (!string.IsNullOrEmpty(mappedData.VehiculoCombustible), false) },
                { "Destino", (!string.IsNullOrEmpty(mappedData.VehiculoDestino), false) },
                { "Categoria", (!string.IsNullOrEmpty(mappedData.VehiculoCategoria), false) }
            };

            return CalculateCategoryMetric(masterDataFieldsMap, "Master Data");
        }

        private CategoryMetric CategorizeOptionalFields(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var optionalFieldsMap = new Dictionary<string, (bool IsMapped, bool IsCritical)>
            {
                { "CorredorNombre", (!string.IsNullOrEmpty(mappedData.CorredorNombre), false) },
                { "CorredorNumero", (!string.IsNullOrEmpty(mappedData.CorredorNumero), false) },
                { "Moneda", (!string.IsNullOrEmpty(mappedData.Moneda), false) }
            };

            return CalculateCategoryMetric(optionalFieldsMap, "Opcional");
        }

        private CategoryMetric CalculateCategoryMetric(Dictionary<string, (bool IsMapped, bool IsCritical)> fieldsMap, string categoryName)
        {
            var totalFields = fieldsMap.Count;
            var mappedFields = fieldsMap.Count(f => f.Value.IsMapped);
            var criticalFields = fieldsMap.Where(f => f.Value.IsCritical);
            var criticalMissing = criticalFields.Where(f => !f.Value.IsMapped).Select(f => f.Key).ToList();
            var successfullyMapped = fieldsMap.Where(f => f.Value.IsMapped).Select(f => f.Key).ToList();

            var completionPercentage = totalFields > 0 ? (decimal)mappedFields / totalFields * 100 : 0;

            return new CategoryMetric
            {
                TotalFields = totalFields,
                MappedFields = mappedFields,
                MissingFields = totalFields - mappedFields,
                CompletionPercentage = completionPercentage,
                AverageConfidence = mappedFields > 0 ? 85.0m : 0, 
                CriticalMissing = criticalMissing,
                SuccessfullyMapped = successfullyMapped
            };
        }

        private ConfidenceBreakdown CalculateConfidenceBreakdown(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var allFields = new[]
            {
                ("NumeroPoliza", !string.IsNullOrEmpty(mappedData.NumeroPoliza), 95m),
                ("FechaDesde", !string.IsNullOrEmpty(mappedData.FechaDesde), 90m),
                ("FechaHasta", !string.IsNullOrEmpty(mappedData.FechaHasta), 90m),
                ("VehiculoMarca", !string.IsNullOrEmpty(mappedData.VehiculoMarca), 88m),
                ("VehiculoModelo", !string.IsNullOrEmpty(mappedData.VehiculoModelo), 85m),
                ("Premio", mappedData.Premio > 0, 92m),
                ("VehiculoAño", mappedData.VehiculoAño > 0, 95m)
            };

            var mappedFieldsWithConfidence = allFields.Where(f => f.Item2).ToList();

            var exactMatches = mappedFieldsWithConfidence.Where(f => f.Item3 >= 90m).ToList();
            var highConfidence = mappedFieldsWithConfidence.Where(f => f.Item3 >= 75m && f.Item3 < 90m).ToList();
            var mediumConfidence = mappedFieldsWithConfidence.Where(f => f.Item3 >= 50m && f.Item3 < 75m).ToList();
            var lowConfidence = mappedFieldsWithConfidence.Where(f => f.Item3 >= 25m && f.Item3 < 50m).ToList();
            var veryLowConfidence = mappedFieldsWithConfidence.Where(f => f.Item3 < 25m).ToList();

            var totalMapped = mappedFieldsWithConfidence.Count;
            var weightedAverage = totalMapped > 0 ? mappedFieldsWithConfidence.Average(f => f.Item3) : 0;

            return new ConfidenceBreakdown
            {
                ExactMatches = new ConfidenceLevel
                {
                    FieldCount = exactMatches.Count,
                    Percentage = totalMapped > 0 ? (decimal)exactMatches.Count / totalMapped * 100 : 0,
                    FieldNames = exactMatches.Select(f => f.Item1).ToList(),
                    AverageConfidence = exactMatches.Any() ? exactMatches.Average(f => f.Item3) : 0
                },
                HighConfidence = new ConfidenceLevel
                {
                    FieldCount = highConfidence.Count,
                    Percentage = totalMapped > 0 ? (decimal)highConfidence.Count / totalMapped * 100 : 0,
                    FieldNames = highConfidence.Select(f => f.Item1).ToList(),
                    AverageConfidence = highConfidence.Any() ? highConfidence.Average(f => f.Item3) : 0
                },
                MediumConfidence = new ConfidenceLevel
                {
                    FieldCount = mediumConfidence.Count,
                    Percentage = totalMapped > 0 ? (decimal)mediumConfidence.Count / totalMapped * 100 : 0,
                    FieldNames = mediumConfidence.Select(f => f.Item1).ToList(),
                    AverageConfidence = mediumConfidence.Any() ? mediumConfidence.Average(f => f.Item3) : 0
                },
                LowConfidence = new ConfidenceLevel
                {
                    FieldCount = lowConfidence.Count,
                    Percentage = totalMapped > 0 ? (decimal)lowConfidence.Count / totalMapped * 100 : 0,
                    FieldNames = lowConfidence.Select(f => f.Item1).ToList(),
                    AverageConfidence = lowConfidence.Any() ? lowConfidence.Average(f => f.Item3) : 0
                },
                VeryLowConfidence = new ConfidenceLevel
                {
                    FieldCount = veryLowConfidence.Count,
                    Percentage = totalMapped > 0 ? (decimal)veryLowConfidence.Count / totalMapped * 100 : 0,
                    FieldNames = veryLowConfidence.Select(f => f.Item1).ToList(),
                    AverageConfidence = veryLowConfidence.Any() ? veryLowConfidence.Average(f => f.Item3) : 0
                },
                WeightedAverageConfidence = weightedAverage,
                OverallConfidenceLevel = DetermineOverallConfidenceLevel(weightedAverage)
            };
        }

        private List<ImprovementSuggestion> GenerateImprovementSuggestions(PolizaDataMapped mappedData, Dictionary<string, object> extractedData)
        {
            var suggestions = new List<ImprovementSuggestion>();

            if (string.IsNullOrEmpty(mappedData.VehiculoMotor))
            {
                suggestions.Add(new ImprovementSuggestion
                {
                    Category = "Vehículo",
                    Title = "Mejorar extracción de número de motor",
                    Description = "El número de motor no se está extrayendo consistentemente",
                    Priority = "Medium",
                    ActionType = "DocumentQuality",
                    SpecificFields = new List<string> { "VehiculoMotor" },
                    PotentialImprovement = 15m
                });
            }

            if (string.IsNullOrEmpty(mappedData.VehiculoCombustible))
            {
                suggestions.Add(new ImprovementSuggestion
                {
                    Category = "Master Data",
                    Title = "Mapeo automático de combustible",
                    Description = "Implementar mapeo automático de tipos de combustible a códigos Velneo",
                    Priority = "High",
                    ActionType = "Configuration",
                    SpecificFields = new List<string> { "VehiculoCombustible" },
                    PotentialImprovement = 20m
                });
            }

            return suggestions;
        }

        private string DetermineOverallConfidenceLevel(decimal averageConfidence)
        {
            return averageConfidence switch
            {
                >= 90m => "Muy Alta",
                >= 75m => "Alta",
                >= 60m => "Media",
                >= 40m => "Baja",
                _ => "Muy Baja"
            };
        }

        private int CountMappedFields(PolizaDataMapped data)
        {
            int count = 0;

            if (!string.IsNullOrEmpty(data.NumeroPoliza)) count++;
            if (!string.IsNullOrEmpty(data.Endoso) && data.Endoso != "0") count++;
            if (!string.IsNullOrEmpty(data.FechaDesde)) count++;
            if (!string.IsNullOrEmpty(data.FechaHasta)) count++;
            if (data.Premio > 0) count++;
            if (data.MontoTotal > 0) count++;
            if (!string.IsNullOrEmpty(data.VehiculoMarca)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoModelo)) count++;
            if (data.VehiculoAño > 0) count++;
            if (!string.IsNullOrEmpty(data.VehiculoMotor)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoChasis)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoCombustible)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoDestino)) count++;
            if (!string.IsNullOrEmpty(data.VehiculoCategoria)) count++;
            if (!string.IsNullOrEmpty(data.MedioPago)) count++;
            if (data.CantidadCuotas > 0) count++;
            if (!string.IsNullOrEmpty(data.TipoMovimiento)) count++;

            return count;
        }

        private string DetermineMappingQuality(decimal completeness)
        {
            return completeness switch
            {
                >= 90 => "Excelente",
                >= 75 => "Buena",
                >= 50 => "Aceptable",
                >= 25 => "Básica",
                _ => "Insuficiente"
            };
        }

        #endregion

        #region Determinación de Completitud

        private bool DetermineIfComplete(
            PolizaDataMapped mappedData,
            CriticalFieldsStatus criticalStatus,
            List<FieldMappingIssue> issues)
        {
            var hasEssentials = !string.IsNullOrEmpty(mappedData.NumeroPoliza) &&
                               mappedData.NumeroPoliza.Length >= 7 &&
                               !string.IsNullOrEmpty(mappedData.FechaDesde) &&
                               !string.IsNullOrEmpty(mappedData.FechaHasta);

            var hasNoErrors = !issues.Any(i => i.Severity == "Error");

            var criticalFieldsOk = criticalStatus.CriticalFieldsCompleteness >= 75;

            return hasEssentials && hasNoErrors && criticalFieldsOk;
        }

        #endregion

        #region Métodos de Extracción (reutilizados del service original)
        private string ExtractPolicyNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.numero", "datos_poliza", "Nº de Póliza",
                "poliza_numero", "numero_poliza"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var match = Regex.Match(value, @"(\d{7,9})");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return "";
        }

        private string ExtractEndorsement(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "poliza.endoso", "endoso", "datos_poliza" };
            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var match = Regex.Match(value, @"Endoso:\s*(\d+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return "0";
        }

        private string ExtractStartDate(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.fecha-desde",        
                "poliza.vigencia.desde",     
                "poliza.fecha_desde",       
                "vigencia_desde",
                "confchdes",
                "datos_poliza",
                "vigencia.desde"
            };
            return ExtractDateFromFields(data, possibleFields);
        }

        private string ExtractEndDate(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.fecha-hasta",      
                "poliza.vigencia.hasta",     
                "poliza.fecha_hasta",        
                "vigencia_hasta",
                "confchhas",
                "datos_poliza",
                "vigencia.hasta"
            };
            return ExtractDateFromFields(data, possibleFields);
        }

        private decimal ExtractFirstInstallmentValue(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "pago.prima_cuota[1]",       
                "pago.cuotas[0].prima",     
                "pago.primera_cuota",        
                "cuota.valor",              
                "cuota.monto"               
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var cleanValue = CleanCurrencyValue(value);
                    if (decimal.TryParse(cleanValue, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogDebug("Valor primera cuota encontrado en {Field}: {Value}", field, amount);
                        return amount;
                    }
                }
            }

            _logger.LogWarning("No se pudo extraer valor de primera cuota");
            return 0m;
        }

        private string CleanCurrencyValue(string input)
        {
            if (string.IsNullOrEmpty(input)) return "0";
            var cleaned = Regex.Replace(input, @"[^\d\.,\-]", "").Trim();

            if (cleaned.Contains(","))
            {
                if (cleaned.Contains(".") && cleaned.LastIndexOf(',') > cleaned.LastIndexOf('.'))
                {
                    cleaned = cleaned.Replace(".", "").Replace(",", ".");
                }
                else if (!cleaned.Contains("."))
                {
                    cleaned = cleaned.Replace(",", ".");
                }
            }
            
            return cleaned;
        }

        private decimal ExtractPremium(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.prima_comercial",           
                "financiero.prima_comercial",    
                "pago.cuotas[0].prima",            
                "prima_comercial", 
                "conpremio", 
                "premio", 
                "datos_financiero"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    _logger.LogInformation("ExtractPremium - Campo encontrado: '{Field}' = '{Value}'", field, value);
                    var amount = ExtractAmountFromString(value);
                    if (amount > 0)
                    {
                        _logger.LogInformation("ExtractPremium - Premio extraído: {Amount} desde campo '{Field}'", amount, field);
                        return amount;
                    }
                }
            }

            _logger.LogWarning("ExtractPremium - No se encontró valor de premio en ningún campo");
            return 0;
        }

        private decimal ExtractAmountFromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            try
            {
                var cleanValue = value
                    .Replace("Prima Comercial:", "")
                    .Replace("Premio Total a Pagar:", "")
                    .Replace("Prima:", "")
                    .Replace("$", "")
                    .Replace("€", "")
                    .Replace("UYU", "")
                    .Replace("USD", "")
                    .Replace("PESOS", "")
                    .Replace("PESO URUGUAYO", "")
                    .Replace("\n", " ")
                    .Replace("\r", " ")
                    .Trim();

                var uruguayanMatch = Regex.Match(cleanValue, @"(\d{1,3}(?:\.\d{3})*,\d{2})");
                if (uruguayanMatch.Success)
                {
                    var uruguayanNumber = uruguayanMatch.Groups[1].Value
                        .Replace(".", "")  
                        .Replace(",", "."); 

                    if (decimal.TryParse(uruguayanNumber, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogInformation("Monto extraído (formato uruguayo): '{Input}' -> {Amount}", value, amount);
                        return amount;
                    }
                }

                var standardMatch = Regex.Match(cleanValue, @"(\d{1,3}(?:,\d{3})*\.\d{2})");
                if (standardMatch.Success)
                {
                    var standardNumber = standardMatch.Groups[1].Value.Replace(",", ""); 

                    if (decimal.TryParse(standardNumber, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogInformation("Monto extraído (formato estándar): '{Input}' -> {Amount}", value, amount);
                        return amount;
                    }
                }

                var anyNumberMatch = Regex.Match(cleanValue, @"(\d+(?:[.,]\d+)?)");
                if (anyNumberMatch.Success)
                {
                    var numberStr = anyNumberMatch.Groups[1].Value.Replace(",", ".");

                    if (decimal.TryParse(numberStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogInformation("Monto extraído (número simple): '{Input}' -> {Amount}", value, amount);
                        return amount;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extrayendo monto de '{Value}': {Error}", value, ex.Message);
            }

            _logger.LogWarning("No se pudo extraer monto de: '{Value}'", value);
            return 0;
        }

        private string ExtractVehicleBrand(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.marca", "marca", "MARCA", "conmaraut"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractVehicleModel(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.modelo", "modelo", "MODELO", "conmodaut"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private int ExtractVehicleYear(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "vehiculo.anio", "vehiculo.año", "año", "AÑO", "conanioaut" };
            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var match = Regex.Match(value, @"\b(20\d{2}|19\d{2})\b");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var year))
                    {
                        return year;
                    }
                }
            }
            return 0;
        }

        private string ExtractMotorNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.motor", "motor", "MOTOR", "numero_motor"
            };
            var motorFull = GetFirstValidValue(data, possibleFields);

            return motorFull.Replace("MOTOR", "").Replace("motor", "").Trim();
        }

        private string ExtractChassisNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.chasis", "chasis", "CHASIS", "numero_chasis"
            };
            var chassisFull = GetFirstValidValue(data, possibleFields);

            return chassisFull.Replace("CHASIS", "").Replace("chasis", "").Trim();
        }

        private string ExtractFuelType(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.combustible", "combustible", "COMBUSTIBLE"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractDestination(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.destino_del_vehiculo", "destino", "DESTINO DEL VEHÍCULO"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractCategory(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.tipo_vehiculo", "categoria", "TIPO DE VEHÍCULO"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        private string ExtractPaymentMethod(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "pago.medio",          
                "pago.forma",
                "forma_pago",
                "payment_method",
                "metodo_pago"
            };
            return GetFirstValidValue(data, possibleFields);
        }
        private string ExtractMovementType(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "tipoMovimiento", "tipo_movimiento", "movement_type",
                "operacion", "operation", "tipo_operacion"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        #endregion

        #region Mapeos a IDs de Velneo (Master Data)

        private async Task<int> FindDepartmentIdAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "asegurado.departamento", "departamento", "depto", "dptnom"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var suggestion = await _masterDataService.SuggestMappingAsync("departamento", value);
                    if (suggestion.Confidence >= 0.8 && int.TryParse(suggestion.SuggestedValue, out var deptId))
                    {
                        return deptId;
                    }
                }
            }
            return 1;
        }

        private async Task<string> FindFuelCodeAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.combustible", "combustible", "COMBUSTIBLE"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var suggestion = await _masterDataService.SuggestMappingAsync("combustible", value);
                    if (suggestion.Confidence >= 0.7)
                    {
                        return suggestion.SuggestedValue ?? "1";
                    }
                }
            }
            return "1";
        }

        private async Task<int> FindDestinationIdAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.destino_del_vehiculo", "destino", "DESTINO DEL VEHÍCULO"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var suggestion = await _masterDataService.SuggestMappingAsync("destino", value);
                    if (suggestion.Confidence >= 0.7 && int.TryParse(suggestion.SuggestedValue, out var destId))
                    {
                        return destId;
                    }
                }
            }
            return 1; 
        }

        private async Task<int> FindCategoryIdAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.tipo_vehiculo", "categoria", "TIPO DE VEHÍCULO"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var suggestion = await _masterDataService.SuggestMappingAsync("categoria", value);
                    if (suggestion.Confidence >= 0.7 && int.TryParse(suggestion.SuggestedValue, out var catId))
                    {
                        return catId;
                    }
                }
            }
            return 1; 
        }

        private async Task<int> FindQualityIdAsync(Dictionary<string, object> data)
        {
            return 1;
        }

        private async Task<int> FindTariffIdAsync(Dictionary<string, object> data)
        {
            return 1;
        }

        #endregion

        #region Mapeo de Códigos

        private string MapPaymentMethodCode(string paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod)) return "1";

            var normalized = paymentMethod.ToUpper();
            return normalized switch
            {
                var x when x.Contains("CONTADO") || x.Contains("EFECTIVO") => "1",     
                var x when x.Contains("TARJETA") || x.Contains("CREDITO") => "2",     
                var x when x.Contains("DEBITO") || x.Contains("BANCARIO") => "3",      
                var x when x.Contains("COBRADOR") => "4",                            
                var x when x.Contains("CONFORME") => "5",                             
                var x when x.Contains("CHEQUE") => "6",                                
                var x when x.Contains("TRANSFERENCIA") => "7",                         
                var x when x.Contains("PASS") || x.Contains("CARD") => "8",           
                _ => "1" 
            };
        }

        #endregion

        #region Validación del Request Velneo

        private async Task ValidateVelneoRequest(VelneoPolizaRequest request)
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

            if (string.IsNullOrEmpty(request.confchdes))
                errors.Add("Fecha de inicio es requerida");

            if (string.IsNullOrEmpty(request.confchhas))
                errors.Add("Fecha de fin es requerida");

            if (!string.IsNullOrEmpty(request.confchdes) && !DateTime.TryParse(request.confchdes, out _))
                errors.Add("Fecha de inicio tiene formato inválido");

            if (!string.IsNullOrEmpty(request.confchhas) && !DateTime.TryParse(request.confchhas, out _))
                errors.Add("Fecha de fin tiene formato inválido");

            if (errors.Any())
            {
                throw new ValidationException($"Errores de validación: {string.Join(", ", errors)}");
            }

            _logger.LogInformation("Request Velneo validado exitosamente");
        }

        #endregion

        #region Métodos Auxiliares

        private bool TryGetValue(Dictionary<string, object> data, string key, out string value)
        {
            value = "";
            if (data.TryGetValue(key, out var obj) && obj != null)
            {
                value = obj.ToString()?.Trim() ?? "";
                return !string.IsNullOrEmpty(value);
            }
            return false;
        }

        private string GetFirstValidValue(Dictionary<string, object> data, string[] possibleFields)
        {
            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var cleaned = CleanText(value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        return cleaned;
                    }
                }
            }
            return "";
        }

        private string ExtractDateFromFields(Dictionary<string, object> data, string[] fields)
        {
            foreach (var field in fields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var date = ParseDateFromText(value);
                    if (!string.IsNullOrEmpty(date))
                    {
                        return date;
                    }
                }
            }
            return DateTime.Today.ToString("yyyy-MM-dd");
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

        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input
                .Trim()
                .Replace("  ", " ")
                .Replace("\n", " ")
                .Replace("\r", "")
                .Replace("\t", " ");
        }

        private string ParseDateFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var patterns = new[]
            {
                @"(\d{1,2})/(\d{1,2})/(\d{4})",
                @"(\d{4})-(\d{1,2})-(\d{1,2})",
                @"(\d{1,2})-(\d{1,2})-(\d{4})"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    try
                    {
                        var day = int.Parse(match.Groups[1].Value);
                        var month = int.Parse(match.Groups[2].Value);
                        var year = int.Parse(match.Groups[3].Value);

                        if (year < 100) year += 2000;
                        if (day > 31) (day, year) = (year, day);

                        var date = new DateTime(year, month, day);
                        return date.ToString("yyyy-MM-dd");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error parseando fecha {Text}: {Error}", text, ex.Message);
                    }
                }
            }
            return "";
        }

        private string GetStringValueWithRenewOverride(string? frontendValue, string extractedValue, string fieldName)
        {
            if (!string.IsNullOrEmpty(frontendValue))
            {
                _logger.LogInformation("Usando override de frontend para {FieldName}: {Value}", fieldName, frontendValue);
                return frontendValue;
            }
            return extractedValue ?? "";
        }

        /// <summary>
        /// Obtiene int con override del frontend
        /// </summary>
        private int GetIntValueWithRenewOverride(string? frontendValue, int extractedValue)
        {
            if (!string.IsNullOrEmpty(frontendValue) && int.TryParse(frontendValue, out var parsedValue))
            {
                _logger.LogInformation("Usando override numérico de frontend: {Value}", parsedValue);
                return parsedValue;
            }
            return extractedValue;
        }

        /// <summary>
        /// Obtiene string con override del frontend de forma async
        /// </summary>
        private async Task<string> GetStringValueWithRenewOverrideAsync(string? frontendValue, Func<Task<string>> extractFunc)
        {
            if (!string.IsNullOrEmpty(frontendValue))
            {
                _logger.LogInformation("Usando override async string de frontend: {Value}", frontendValue);
                return frontendValue;
            }
            return await extractFunc();
        }

        /// <summary>
        /// Obtiene int con override del frontend de forma async
        /// </summary>
        private async Task<int> GetIntValueWithRenewOverrideAsync(string? frontendValue, Func<Task<int>> extractFunc)
        {
            if (!string.IsNullOrEmpty(frontendValue) && int.TryParse(frontendValue, out var parsedValue))
            {
                _logger.LogInformation("Usando override async int de frontend: {Value}", parsedValue);
                return parsedValue;
            }
            return await extractFunc();
        }

        /// <summary>
        /// Formatea observaciones específicas para renovación
        /// </summary>
        private string FormatRenovationObservations(RenewPolizaRequest renewRequest, VelneoPolizaRequest request, Dictionary<string, object> normalizedData, string? polizaAnteriorNumero = null)
        {
            var observations = new List<string>();

            // ✅ OBSERVACIONES PRINCIPALES MÁS SIMPLES
            var numeroPolizaAnterior = polizaAnteriorNumero ?? "N/A";
            observations.Add($"Renovacion de Poliza {numeroPolizaAnterior} (ID: {renewRequest.PolizaAnteriorId})");

            // ✅ CRONOGRAMA DE CUOTAS
            if (request.concuo > 1 && request.contot > 0)
            {
                observations.Add(""); // Línea en blanco
                observations.Add("CRONOGRAMA DE CUOTAS:");

                var valorCuota = Math.Round((decimal)request.contot / request.concuo, 2);
                var fechaBase = DateTime.TryParse(request.confchdes, out var fechaInicio) ? fechaInicio : DateTime.Now;

                for (int i = 1; i <= request.concuo; i++)
                {
                    var fechaCuota = fechaBase.AddMonths(i - 1);
                    var montoCuota = (i == request.concuo)
                        ? request.contot - (valorCuota * (request.concuo - 1)) // Última cuota ajusta diferencia
                        : valorCuota;

                    observations.Add($"Cuota {i:00}: {fechaCuota:dd/MM/yyyy} - ${montoCuota:N2}");
                }

                observations.Add($"TOTAL: ${request.contot:N2} en {request.concuo} cuotas");
            }
            else if (request.concuo == 1)
            {
                observations.Add($"Pago contado: ${request.contot:N2}");
            }

            // ✅ OBSERVACIONES DEL USUARIO (si las hay)
            if (!string.IsNullOrEmpty(renewRequest.Observaciones) &&
                !renewRequest.Observaciones.Contains("Renovación automática"))
            {
                observations.Add(""); // Línea en blanco
                observations.Add("NOTAS ADICIONALES:");
                observations.Add(renewRequest.Observaciones);
            }

            // ✅ COMENTARIOS DEL USUARIO (si los hay)
            if (!string.IsNullOrEmpty(renewRequest.ComentariosUsuario))
            {
                observations.Add(""); // Línea en blanco
                observations.Add("COMENTARIOS:");
                observations.Add(renewRequest.ComentariosUsuario);
            }

            // ✅ CAMPOS CORREGIDOS (si los hay)
            if (renewRequest.CamposCorregidos != null && renewRequest.CamposCorregidos.Count > 0)
            {
                observations.Add(""); // Línea en blanco
                observations.Add($"Campos corregidos: {string.Join(", ", renewRequest.CamposCorregidos)}");
            }

            return string.Join("\n", observations);
        }

        private void LogAppliedOverrides(RenewPolizaRequest renewRequest)
        {
            var overrides = new List<string>();

            if (!string.IsNullOrEmpty(renewRequest.CombustibleId)) overrides.Add($"Combustible: {renewRequest.CombustibleId}");
            if (!string.IsNullOrEmpty(renewRequest.CategoriaId)) overrides.Add($"Categoría: {renewRequest.CategoriaId}");
            if (!string.IsNullOrEmpty(renewRequest.CalidadId)) overrides.Add($"Calidad: {renewRequest.CalidadId}");
            if (!string.IsNullOrEmpty(renewRequest.DestinoId)) overrides.Add($"Destino: {renewRequest.DestinoId}");
            if (!string.IsNullOrEmpty(renewRequest.DepartamentoId)) overrides.Add($"Departamento: {renewRequest.DepartamentoId}");
            if (!string.IsNullOrEmpty(renewRequest.TarifaId)) overrides.Add($"Tarifa: {renewRequest.TarifaId}");

            if (overrides.Count > 0)
            {
                _logger.LogInformation("RENOVACIÓN - Overrides de master data aplicados: {Overrides}", string.Join(", ", overrides));
            }
            else
            {
                _logger.LogInformation("RENOVACIÓN - Sin overrides de master data, usando mapeo automático");
            }
        }

        private string? ExtractPolizaNumber(object? polizaAnterior)
        {
            if (polizaAnterior == null) return null;

            try
            {
                // Intentar acceder a la propiedad 'conpol' por reflexión
                var type = polizaAnterior.GetType();
                var property = type.GetProperty("conpol");

                if (property != null)
                {
                    return property.GetValue(polizaAnterior)?.ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extrayendo número de póliza anterior: {Error}", ex.Message);
                return null;
            }
        }

        #endregion
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}