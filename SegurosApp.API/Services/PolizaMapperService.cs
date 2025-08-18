using SegurosApp.API.DTOs.Velneo.Request;
using SegurosApp.API.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services
{
    public class PolizaMapperService
    {
        private readonly IVelneoMasterDataService _masterDataService;
        private readonly ILogger<PolizaMapperService> _logger;

        public PolizaMapperService(
            IVelneoMasterDataService masterDataService,
            ILogger<PolizaMapperService> logger)
        {
            _masterDataService = masterDataService;
            _logger = logger;
        }

        /// <summary>
        /// 🎯 Convierte datos escaneados a estructura Velneo con mapeo inteligente
        /// </summary>
        public async Task<CreatePolizaVelneoRequest> MapToVelneoAsync(
            Dictionary<string, object> extractedData,
            int scanId,
            int userId)
        {
            _logger.LogInformation("🔄 Iniciando mapeo de datos escaneados a Velneo para scan {ScanId}", scanId);

            var velneoRequest = new CreatePolizaVelneoRequest
            {
                id = 0, // Lo asigna Velneo

                // ✅ CAMPOS BÁSICOS DE LA PÓLIZA
                conpol = ExtractPolicyNumber(extractedData),
                conend = ExtractEndorsement(extractedData),
                confchdes = ExtractStartDate(extractedData),
                confchhas = ExtractEndDate(extractedData),
                conpremio = ExtractPremium(extractedData),
                contot = ExtractTotalAmount(extractedData),

                // ✅ DATOS DEL VEHÍCULO (texto directo)
                conmaraut = ExtractVehicleBrand(extractedData),
                conmotor = ExtractMotorNumber(extractedData),
                conchasis = ExtractChassisNumber(extractedData),
                conanioaut = ExtractVehicleYear(extractedData),

                // ✅ MAPEO A IDs Y CÓDIGOS (requiere búsqueda)
                clinro = await FindClientIdAsync(extractedData),
                corrnom = await FindBrokerIdAsync(extractedData),
                dptnom = await FindDepartmentIdAsync(extractedData),
                combustibles = await FindFuelCodeAsync(extractedData),
                desdsc = await FindDestinationIdAsync(extractedData),
                catdsc = await FindCategoryIdAsync(extractedData),
                caldsc = await FindQualityIdAsync(extractedData),
                tarcod = await FindTariffIdAsync(extractedData),

                // ✅ FORMA DE PAGO Y CUOTAS
                consta = MapPaymentMethod(extractedData),
                concuo = ExtractInstallmentCount(extractedData),

                // ✅ DATOS DE GESTIÓN (valores por defecto inteligentes)
                congesti = "1", // En emisión
                contra = MapMovementType(extractedData),
                convig = "1", // Vigente
                moncod = 858, // Peso uruguayo (ISO)

                // ✅ METADATOS
                ingresado = DateTime.UtcNow,
                last_update = DateTime.UtcNow,
                app_id = scanId // Referencia al escaneo original
            };

            await LogMappingResults(velneoRequest, extractedData);
            return velneoRequest;
        }

        #region Extracción de Campos Básicos

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
                    // Extraer número de póliza del texto
                    var match = Regex.Match(value, @"(\d{7,9})");
                    if (match.Success)
                    {
                        _logger.LogInformation("✅ Número de póliza extraído: {PolicyNumber}", match.Groups[1].Value);
                        return match.Groups[1].Value;
                    }
                }
            }

            _logger.LogWarning("⚠️ No se pudo extraer número de póliza");
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

            return "0"; // Default para nueva emisión
        }

        private string ExtractStartDate(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.vigencia.desde", "vigencia_desde", "confchdes",
                "datos_poliza", "vigencia.desde"
            };

            return ExtractDateFromFields(data, possibleFields, "fecha de inicio");
        }

        private string ExtractEndDate(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.vigencia.hasta", "vigencia_hasta", "confchhas",
                "datos_poliza", "vigencia.hasta"
            };

            return ExtractDateFromFields(data, possibleFields, "fecha de fin");
        }

        private int ExtractPremium(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "poliza.prima_comercial", "prima_comercial", "conpremio",
                "financiero.prima_comercial", "datos_poliza"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var amount = ExtractAmountFromText(value);
                    if (amount > 0)
                    {
                        _logger.LogInformation("✅ Premio extraído: {Premium}", amount);
                        return (int)amount;
                    }
                }
            }

            _logger.LogWarning("⚠️ No se pudo extraer premio");
            return 0;
        }

        private int ExtractTotalAmount(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "financiero.premio_total", "premio_total", "contot",
                "PREMIO TOTAL A PAGAR", "datos_poliza"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var amount = ExtractAmountFromText(value);
                    if (amount > 0)
                    {
                        return (int)amount;
                    }
                }
            }

            return 0;
        }

        private string ExtractVehicleBrand(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.marca", "marca", "MARCA", "conmaraut"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var brand = CleanVehicleBrand(value);
                    if (!string.IsNullOrEmpty(brand))
                    {
                        _logger.LogInformation("✅ Marca extraída: {Brand}", brand);
                        return brand;
                    }
                }
            }

            return "";
        }

        private string ExtractMotorNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "vehiculo.motor", "motor", "MOTOR", "conmotor" };
            return ExtractFirstValidValue(data, possibleFields, "número de motor");
        }

        private string ExtractChassisNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "vehiculo.chasis", "chasis", "CHASIS", "conchasis" };
            return ExtractFirstValidValue(data, possibleFields, "número de chasis");
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
                        _logger.LogInformation("✅ Año extraído: {Year}", year);
                        return year;
                    }
                }
            }

            return 0;
        }

        private int ExtractInstallmentCount(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "pago.modo_facturacion", "modo_facturacion", "cuotas", "concuo"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var match = Regex.Match(value, @"(\d{1,2})\s*cuotas?", RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                    {
                        _logger.LogInformation("✅ Cantidad de cuotas extraída: {Count}", count);
                        return count;
                    }
                }
            }

            return 1; // Default: contado
        }

        #endregion

        #region Mapeo a IDs y Códigos (con búsqueda en master data)

        private async Task<int> FindClientIdAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "asegurado.nombre", "cliente_nombre", "clinro", "clinom"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var clientName = CleanClientName(value);
                    if (!string.IsNullOrEmpty(clientName))
                    {
                        // TODO: Implementar búsqueda de cliente en Velneo
                        _logger.LogInformation("🔍 Buscando cliente: {ClientName}", clientName);
                        // return await _velneoClientService.FindClientByNameAsync(clientName);
                    }
                }
            }

            return 0; // No encontrado
        }

        private async Task<int> FindBrokerIdAsync(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "corredor.nombre", "corredor.numero", "corrnom"
            };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    // Si es número, intentar convertir directamente
                    var numberMatch = Regex.Match(value, @"(\d+)");
                    if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out var brokerId))
                    {
                        _logger.LogInformation("✅ ID de corredor extraído: {BrokerId}", brokerId);
                        return brokerId;
                    }

                    // Si es nombre, buscar por nombre
                    var masterData = await _masterDataService.GetAllMasterDataAsync();
                    var brokerName = CleanBrokerName(value);
                    var broker = masterData.Corredores.FirstOrDefault(c =>
                        c.corrnom.Contains(brokerName, StringComparison.OrdinalIgnoreCase));

                    if (broker != null)
                    {
                        _logger.LogInformation("✅ Corredor encontrado: {BrokerId} - {BrokerName}", broker.id, broker.corrnom);
                        return broker.id;
                    }
                }
            }

            return 0;
        }

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
                        _logger.LogInformation("✅ Departamento mapeado: {DeptName} → {DeptId} (confianza: {Confidence:P1})",
                            value, deptId, suggestion.Confidence);
                        return deptId;
                    }
                }
            }

            return 1; // Default: Montevideo
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
                        _logger.LogInformation("✅ Combustible mapeado: {FuelName} → {FuelCode} (confianza: {Confidence:P1})",
                            value, suggestion.SuggestedValue, suggestion.Confidence);
                        return suggestion.SuggestedValue ?? "1";
                    }
                }
            }

            return "1"; // Default
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

            return 1; // Default
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

            return 1; // Default
        }

        private async Task<int> FindQualityIdAsync(Dictionary<string, object> data)
        {
            // Por ahora valor por defecto, se puede implementar lógica específica
            return 1;
        }

        private async Task<int> FindTariffIdAsync(Dictionary<string, object> data)
        {
            // Por ahora valor por defecto, se puede implementar lógica específica
            return 1;
        }

        #endregion

        #region Mapeo de Códigos Estáticos

        private string MapPaymentMethod(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "pago.medio", "medio_pago", "forma_pago", "consta" };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var normalizedPayment = value.ToUpper();

                    if (normalizedPayment.Contains("TARJETA") || normalizedPayment.Contains("CREDITO"))
                        return "T";
                    if (normalizedPayment.Contains("CONTADO") || normalizedPayment.Contains("EFECTIVO"))
                        return "1";
                    if (normalizedPayment.Contains("DEBITO") || normalizedPayment.Contains("BANCARIO"))
                        return "B";
                }
            }

            return "1"; // Default: Contado
        }

        private string MapMovementType(Dictionary<string, object> data)
        {
            var possibleFields = new[] { "poliza.tipo_movimiento", "tipo_movimiento", "contra" };

            foreach (var field in possibleFields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var normalized = value.ToUpper();

                    if (normalized.Contains("EMISION") || normalized.Contains("NUEVA"))
                        return "1"; // Nuevo
                    if (normalized.Contains("RENOVACION"))
                        return "2"; // Renovación
                    if (normalized.Contains("ENDOSO"))
                        return "4"; // Endoso
                }
            }

            return "1"; // Default: Nuevo
        }

        #endregion

        #region Métodos de Utilidad

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

        private string ExtractDateFromFields(Dictionary<string, object> data, string[] fields, string fieldType)
        {
            foreach (var field in fields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var date = ParseDateFromText(value);
                    if (!string.IsNullOrEmpty(date))
                    {
                        _logger.LogInformation("✅ {FieldType} extraída: {Date}", fieldType, date);
                        return date;
                    }
                }
            }

            _logger.LogWarning("⚠️ No se pudo extraer {FieldType}", fieldType);
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        private string ExtractFirstValidValue(Dictionary<string, object> data, string[] fields, string fieldType)
        {
            foreach (var field in fields)
            {
                if (TryGetValue(data, field, out var value))
                {
                    var cleaned = CleanAlphanumeric(value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        _logger.LogInformation("✅ {FieldType} extraído: {Value}", fieldType, cleaned);
                        return cleaned;
                    }
                }
            }

            return "";
        }

        private decimal ExtractAmountFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var match = Regex.Match(text, @"[\$\s]*([0-9]+(?:[.,][0-9]{3})*(?:[.,][0-9]{1,2})?)");
            if (match.Success)
            {
                var amountStr = match.Groups[1].Value
                    .Replace(".", "")  // Remover separadores de miles
                    .Replace(",", "."); // Convertir coma decimal a punto

                if (decimal.TryParse(amountStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
                {
                    return amount;
                }
            }

            return 0;
        }

        private string ParseDateFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var patterns = new[]
            {
                @"(\d{1,2})/(\d{1,2})/(\d{4})",  // dd/MM/yyyy
                @"(\d{4})-(\d{1,2})-(\d{1,2})",  // yyyy-MM-dd
                @"(\d{1,2})-(\d{1,2})-(\d{4})"   // dd-MM-yyyy
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

                        // Si el año está en la primera posición, ajustar
                        if (year < 100) // Formato yy
                        {
                            year += 2000;
                        }
                        if (day > 31) // Probablemente yyyy está en la primera posición
                        {
                            (day, year) = (year, day);
                        }

                        var date = new DateTime(year, month, day);
                        return date.ToString("yyyy-MM-dd");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("⚠️ Error parseando fecha {Text}: {Error}", text, ex.Message);
                    }
                }
            }

            return "";
        }

        private string CleanVehicleBrand(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var match = Regex.Match(text, @"(?:MARCA\s*:?\s*)?([A-Z]+(?:\s+[A-Z]+)*)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
        }

        private string CleanClientName(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var match = Regex.Match(text, @"(?:Asegurado\s*:?\s*)?([A-Z\s]+(?:\s+S\.?A\.?)?)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
        }

        private string CleanBrokerName(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var match = Regex.Match(text, @"(?:Nombre\s*:?\s*)?([A-Z\s]+(?:\s+S\.?A\.?)?)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
        }

        private string CleanAlphanumeric(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            return Regex.Replace(text, @"[^A-Z0-9]", "", RegexOptions.IgnoreCase).Trim();
        }

        private async Task LogMappingResults(CreatePolizaVelneoRequest request, Dictionary<string, object> originalData)
        {
            _logger.LogInformation("📊 Resultado del mapeo:");
            _logger.LogInformation("  Póliza: {Policy}", request.conpol);
            _logger.LogInformation("  Cliente ID: {ClientId}", request.clinro);
            _logger.LogInformation("  Corredor ID: {BrokerId}", request.corrnom);
            _logger.LogInformation("  Marca: {Brand}", request.conmaraut);
            _logger.LogInformation("  Año: {Year}", request.conanioaut);
            _logger.LogInformation("  Premio: {Premium}", request.conpremio);
            _logger.LogInformation("  Campos originales procesados: {Count}", originalData.Count);
        }

        #endregion
    }

    #region DTO para Velneo Request

    public class CreatePolizaVelneoRequest
    {
        public int id { get; set; } = 0;
        public string conpol { get; set; } = "";  // Número de póliza
        public string conend { get; set; } = "0"; // Endoso
        public string confchdes { get; set; } = ""; // Fecha desde
        public string confchhas { get; set; } = ""; // Fecha hasta
        public int conpremio { get; set; } = 0;   // Premio
        public int contot { get; set; } = 0;      // Total
        public string conmaraut { get; set; } = ""; // Marca
        public string conmotor { get; set; } = "";  // Motor
        public string conchasis { get; set; } = ""; // Chasis
        public int conanioaut { get; set; } = 0;    // Año
        public int clinro { get; set; } = 0;        // Cliente ID
        public int corrnom { get; set; } = 0;       // Corredor ID
        public int dptnom { get; set; } = 0;        // Departamento ID
        public string combustibles { get; set; } = "1"; // Combustible código
        public int desdsc { get; set; } = 0;        // Destino ID
        public int catdsc { get; set; } = 0;        // Categoría ID
        public int caldsc { get; set; } = 0;        // Calidad ID
        public int tarcod { get; set; } = 0;        // Tarifa ID
        public string consta { get; set; } = "1";   // Forma de pago
        public int concuo { get; set; } = 1;        // Cuotas
        public string congesti { get; set; } = "1"; // Estado gestión
        public string contra { get; set; } = "1";   // Tramite
        public string convig { get; set; } = "1";   // Vigencia
        public int moncod { get; set; } = 858;      // Moneda (peso uruguayo)
        public DateTime ingresado { get; set; } = DateTime.UtcNow;
        public DateTime last_update { get; set; } = DateTime.UtcNow;
        public int app_id { get; set; } = 0;        // Referencia al scan
    }

    #endregion
}