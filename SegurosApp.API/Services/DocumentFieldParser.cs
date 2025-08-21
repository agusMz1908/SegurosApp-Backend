using System.Globalization;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services
{
    public class DocumentFieldParser
    {
        private readonly ILogger<DocumentFieldParser> _logger;

        public DocumentFieldParser(ILogger<DocumentFieldParser> logger)
        {
            _logger = logger;
        }

        public Dictionary<string, object> ProcessExtractedData(Dictionary<string, object> rawFields)
        {
            var processedData = new Dictionary<string, object>();

            try
            {
                _logger.LogInformation("🧠 Iniciando extracción inteligente de {CamposCount} campos", rawFields.Count);

                var camposExtraidos = rawFields.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? ""
                );

                processedData["numeroPoliza"] = ProcessPolicyNumber(camposExtraidos);
                processedData["asegurado"] = ProcessInsuredName(camposExtraidos);
                processedData["documento"] = ProcessDocument(camposExtraidos);
                processedData["vehiculo"] = ProcessVehicle(camposExtraidos);
                processedData["marca"] = ProcessBrand(camposExtraidos);
                processedData["modelo"] = ProcessModel(camposExtraidos);
                processedData["matricula"] = ProcessPlate(camposExtraidos);
                processedData["motor"] = ProcessEngineNumber(camposExtraidos);
                processedData["chasis"] = ProcessChasisNumber(camposExtraidos);
                processedData["vigenciaDesde"] = ProcessStartDate(camposExtraidos);
                processedData["vigenciaHasta"] = ProcessEndDate(camposExtraidos);
                processedData["premio"] = ProcessPremium(camposExtraidos);
                processedData["primaComercial"] = ProcessCommercialPremium(camposExtraidos);
                processedData["compania"] = ProcessInsuranceCompany(camposExtraidos);
                processedData["tipoMovimiento"] = ProcessMovementType(camposExtraidos);
                processedData["endoso"] = ProcessEndorsement(camposExtraidos);
                processedData["formaPago"] = ProcessPaymentMethod(camposExtraidos);
                processedData["cantidadCuotas"] = ProcessInstallmentCount(camposExtraidos);
                processedData["ramo"] = ProcessBranch(camposExtraidos);
                processedData["plan"] = ProcessPlan(camposExtraidos);
                processedData["corredor"] = ProcessBroker(camposExtraidos);

                foreach (var field in rawFields)
                {
                    if (!processedData.ContainsKey(field.Key))
                    {
                        processedData[field.Key] = field.Value;
                    }
                }

                _logger.LogInformation("✅ Datos procesados exitosamente: {ProcessedFields} campos",
                    processedData.Count);

                return processedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando datos extraídos");
                return rawFields;
            }
        }

        #region Procesamiento de Campos Principales

        private string ProcessPolicyNumber(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "numeroPoliza", "numero_poliza", "poliza_numero", "policy_number",
                "poliza", "numero", "certificado", "certificate_number", "poliza.numero"
            };

            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var cleaned = CleanPolicyNumber(value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        _logger.LogDebug("✅ Número de póliza encontrado: {PolicyNumber}", cleaned);
                        return cleaned;
                    }
                }
            }

            foreach (var field in fields.Values)
            {
                var text = field?.ToString() ?? "";
                var match = Regex.Match(text, @"(?:Póliza|Poliza|Certificado|Policy)\s*(?:N°|No\.|#)?\s*:?\s*([A-Z0-9\-/]+)",
                    RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var cleaned = CleanPolicyNumber(match.Groups[1].Value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        _logger.LogDebug("✅ Número de póliza extraído por regex: {PolicyNumber}", cleaned);
                        return cleaned;
                    }
                }
            }

            return "";
        }

        private string ProcessInsuredName(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "asegurado", "asegurado_nombre", "cliente_nombre", "insured_name",
                "cliente", "tomador", "contratante", "beneficiario", "datos_asegurado"
            };

            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var cleaned = ExtractNameFromText(value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        return cleaned;
                    }
                }
            }

            return "";
        }

        private string ProcessDocument(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "documento", "cedula", "rut", "ci", "dni", "document_number",
                "identification", "id_number"
            };

            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var cleaned = CleanDocumentNumber(value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        return cleaned;
                    }
                }
            }

            return "";
        }

        private string ProcessVehicle(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "vehiculo", "vehiculo_descripcion", "vehicle_description", "auto",
                "automovil", "vehicle", "description", "datos_vehiculo"
            };

            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var cleaned = ExtractVehicleFromText(value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        return cleaned;
                    }
                }
            }

            var marca = ProcessBrand(fields);
            var modelo = ProcessModel(fields);
            var año = ProcessYear(fields);

            if (!string.IsNullOrEmpty(marca) || !string.IsNullOrEmpty(modelo))
            {
                var parts = new[] { marca, modelo, año }.Where(p => !string.IsNullOrEmpty(p));
                return string.Join(" ", parts);
            }

            return "";
        }

        private string ProcessBrand(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "marca", "brand", "make", "fabricante", "manufacturer"
            };

            return GetFirstValidField(fields, possibleFields, ExtractBrandFromText);
        }

        private string ProcessModel(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "modelo", "model", "version", "variant"
            };

            return GetFirstValidField(fields, possibleFields, CleanText);
        }

        private string ProcessYear(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "año", "year", "año_fabricacion", "year_manufacture", "modelo_año", "anio"
            };

            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var year = ExtractYear(value);
                    if (!string.IsNullOrEmpty(year))
                    {
                        return year;
                    }
                }
            }

            return "";
        }

        private string ProcessPlate(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "matricula", "placa", "plate", "license_plate", "registration", "padron"
            };

            return GetFirstValidField(fields, possibleFields, CleanPlateNumber);
        }

        private string ProcessEngineNumber(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "motor", "engine", "engine_number", "numero_motor"
            };

            return GetFirstValidField(fields, possibleFields, CleanEngineNumber);
        }

        private string ProcessChasisNumber(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "chasis", "chassis", "vin", "chassis_number", "numero_chasis"
            };

            return GetFirstValidField(fields, possibleFields, CleanChasisNumber);
        }

        private string ProcessStartDate(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "vigenciaDesde", "vigencia_desde", "vigencia_inicio", "fecha_inicio",
                "effective_date", "start_date", "desde"
            };

            return GetFirstValidField(fields, possibleFields, ParseDate);
        }

        private string ProcessEndDate(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "vigenciaHasta", "vigencia_hasta", "vigencia_fin", "fecha_fin",
                "expiry_date", "end_date", "hasta"
            };

            return GetFirstValidField(fields, possibleFields, ParseDate);
        }

        private string ProcessPremium(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "premio", "premio_total", "total_premium", "prima_total",
                "amount", "total", "monto_total"
            };

            return GetFirstValidField(fields, possibleFields, ParseCurrency);
        }

        private string ProcessCommercialPremium(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "primaComercial", "prima_comercial", "commercial_premium",
                "prima", "premium"
            };

            return GetFirstValidField(fields, possibleFields, ParseCurrency);
        }

        private string ProcessInsuranceCompany(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "compania", "company", "aseguradora", "insurer", "insurance_company"
            };

            return GetFirstValidField(fields, possibleFields, CleanCompanyName);
        }

        private string ProcessMovementType(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "tipoMovimiento", "tipo_movimiento", "movement_type",
                "operacion", "operation", "tipo_operacion"
            };

            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var normalized = NormalizeMovementType(value);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        return normalized;
                    }
                }
            }

            return "EMISION"; 
        }

        private string ProcessEndorsement(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "endoso", "endorsement", "numero_endoso", "endorsement_number"
            };

            return GetFirstValidField(fields, possibleFields, s => s.Trim()) ?? "0";
        }

        private string ProcessPaymentMethod(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "formaPago", "forma_pago", "payment_method", "metodo_pago"
            };

            return GetFirstValidField(fields, possibleFields, NormalizePaymentMethod);
        }

        private string ProcessInstallmentCount(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "cantidadCuotas", "cantidad_cuotas", "installments", "cuotas"
            };

            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var count = ExtractNumber(value);
                    if (count > 0)
                    {
                        return count.ToString();
                    }
                }
            }

            return "1"; 
        }

        private string ProcessBranch(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "ramo", "branch", "line_of_business"
            };

            return GetFirstValidField(fields, possibleFields, s => s.Trim()) ?? "AUTOMOVILES";
        }

        private string ProcessPlan(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "plan", "coverage_plan", "poliza.plan"
            };

            return GetFirstValidField(fields, possibleFields, CleanText);
        }

        private string ProcessBroker(Dictionary<string, string> fields)
        {
            var possibleFields = new[] {
                "corredor", "broker", "agent", "datos_corredor"
            };

            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var cleaned = ExtractBrokerFromText(value);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        return cleaned;
                    }
                }
            }

            return "";
        }

        #endregion

        #region Métodos de Extracción de Texto

        private string ExtractNameFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var match = Regex.Match(text, @"(?:Nombre|Name|Asegurado):\s*([^:]+?)(?:\s+(?:Documento|Doc|CI|RUT):|$)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return CleanPersonName(match.Groups[1].Value);
            }

            return CleanPersonName(text);
        }

        private string ExtractVehicleFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var match = Regex.Match(text, @"(?:Marca|MARCA):\s*([^:]+?)(?:\s+(?:Modelo|MODELO):\s*([^:]+?))?(?:\s+(?:Año|AÑO):\s*(\d{4}))?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var parts = new[] {
                    match.Groups[1].Value.Trim(),
                    match.Groups[2].Value.Trim(),
                    match.Groups[3].Value.Trim()
                }.Where(p => !string.IsNullOrEmpty(p));

                return string.Join(" ", parts);
            }

            return CleanText(text);
        }

        private string ExtractBrandFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var match = Regex.Match(text, @"(?:Marca|MARCA):\s*([^:]+?)(?:\s+(?:Modelo|MODELO)|$)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return CleanText(match.Groups[1].Value);
            }

            return CleanText(text);
        }

        private string ExtractBrokerFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var match = Regex.Match(text, @"(?:Nombre|Name):\s*([^N]+?)(?:\s+(?:Número|Number):|$)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return CleanText(match.Groups[1].Value);
            }

            return CleanText(text);
        }

        #endregion

        #region Métodos Auxiliares de Limpieza

        private bool TryGetFieldValue(Dictionary<string, string> fields, string fieldName, out string value)
        {
            value = "";

            if (fields.TryGetValue(fieldName, out var exactValue))
            {
                value = exactValue?.ToString()?.Trim() ?? "";
                return !string.IsNullOrEmpty(value);
            }

            var kvp = fields.FirstOrDefault(f =>
                string.Equals(f.Key, fieldName, StringComparison.OrdinalIgnoreCase));

            if (!kvp.Equals(default(KeyValuePair<string, string>)))
            {
                value = kvp.Value?.ToString()?.Trim() ?? "";
                return !string.IsNullOrEmpty(value);
            }

            return false;
        }

        private string GetFirstValidField(Dictionary<string, string> fields, string[] possibleFields, Func<string, string> processor)
        {
            foreach (var fieldName in possibleFields)
            {
                if (TryGetFieldValue(fields, fieldName, out var value))
                {
                    var processed = processor(value);
                    if (!string.IsNullOrEmpty(processed))
                    {
                        return processed;
                    }
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

        private string CleanPersonName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return CleanText(input).ToTitleCase();
        }

        private string CleanDocumentNumber(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return Regex.Replace(input, @"[^\d\-\.]", "").Trim();
        }

        private string CleanPolicyNumber(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input
                .Replace("Póliza:", "")
                .Replace("Poliza:", "")
                .Replace("Certificado:", "")
                .Replace("Policy:", "")
                .Replace("N°:", "")
                .Replace("No.:", "")
                .Replace("#:", "")
                .Trim()
                .ToUpperInvariant();
        }

        private string ExtractYear(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var match = Regex.Match(input, @"\b(19|20)\d{2}\b");
            return match.Success ? match.Value : "";
        }

        private string CleanPlateNumber(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input
                .Replace(" ", "")
                .Replace("-", "")
                .Trim()
                .ToUpperInvariant();
        }

        private string CleanEngineNumber(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input.Trim().ToUpperInvariant();
        }

        private string CleanChasisNumber(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input.Trim().ToUpperInvariant();
        }

        private string ParseDate(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var formats = new[]
            {
                "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "yyyy/MM/dd",
                "dd-MM-yyyy", "dd.MM.yyyy", "dd/MM/yy", "MM/dd/yy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(input.Trim(), format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                {
                    return date.ToString("yyyy-MM-dd");
                }
            }

            if (DateTime.TryParse(input.Trim(), out var genericDate))
            {
                return genericDate.ToString("yyyy-MM-dd");
            }

            return "";
        }

        private string ParseCurrency(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var cleaned = Regex.Replace(input, @"[^\d\.,\-]", "").Trim();

            if (decimal.TryParse(cleaned, NumberStyles.Currency,
                CultureInfo.CurrentCulture, out var amount))
            {
                return amount.ToString("0.00");
            }

            return "";
        }

        private string CleanCompanyName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return CleanText(input).ToTitleCase();
        }

        private string NormalizeMovementType(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var normalized = input.Trim().ToUpperInvariant();

            return normalized switch
            {
                var x when x.Contains("EMISION") || x.Contains("NUEVA") || x.Contains("ALTA") => "EMISION",
                var x when x.Contains("RENOVACION") || x.Contains("RENEWAL") => "RENOVACION",
                var x when x.Contains("ENDOSO") || x.Contains("MODIFICACION") => "ENDOSO",
                var x when x.Contains("ANULACION") || x.Contains("CANCELACION") => "ANULACION",
                _ => normalized
            };
        }

        private string NormalizePaymentMethod(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var normalized = input.Trim().ToUpperInvariant();

            return normalized switch
            {
                var x when x.Contains("CONTADO") => "CONTADO",
                var x when x.Contains("CREDITO") => "CREDITO",
                var x when x.Contains("TARJETA") => "TARJETA",
                var x when x.Contains("TRANSFERENCIA") => "TRANSFERENCIA",
                var x when x.Contains("DEBITO") => "DEBITO AUTOMATICO",
                _ => normalized
            };
        }

        private int ExtractNumber(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;

            var match = Regex.Match(input, @"\d+");
            return match.Success && int.TryParse(match.Value, out var number) ? number : 0;
        }

        #endregion
    }

    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLowerInvariant());
        }
    }
}