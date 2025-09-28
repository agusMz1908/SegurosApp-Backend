using SegurosApp.API.DTOs;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services.Poliza.Shared
{
    public class PolizaDataExtractor
    {
        private readonly ILogger<PolizaDataExtractor> _logger;

        public PolizaDataExtractor(ILogger<PolizaDataExtractor> logger)
        {
            _logger = logger;
        }

        #region Datos Básicos de Póliza

        public string ExtractPolicyNumber(Dictionary<string, object> data)
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
                        _logger.LogDebug("Número de póliza extraído de '{Field}': {Number}", field, match.Groups[1].Value);
                        return match.Groups[1].Value;
                    }
                }
            }
            return "";
        }

        public string ExtractEndorsement(Dictionary<string, object> data)
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

        public string ExtractStartDate(Dictionary<string, object> data)
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

        public string ExtractEndDate(Dictionary<string, object> data)
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

        #endregion

        #region Datos del Vehículo

        public string ExtractVehicleBrand(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.marca", "marca", "MARCA", "conmaraut"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        public string ExtractVehicleModel(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.modelo", "modelo", "MODELO", "conmodaut"
            };
            return GetFirstValidValue(data, possibleFields);
        }

        public int ExtractVehicleYear(Dictionary<string, object> data)
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

        public string ExtractVehiclePlate(Dictionary<string, object> data)
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
                    _logger.LogInformation("Patente encontrada en datos: {Patente}", realPatente);
                    return realPatente;
                }
            }

            return CleanPatenteValue(value);
        }

        public string ExtractMotorNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.motor", "motor", "MOTOR", "numero_motor"
            };
            var motorFull = GetFirstValidValue(data, possibleFields);

            return motorFull.Replace("MOTOR", "").Replace("motor", "").Trim();
        }

        public string ExtractChassisNumber(Dictionary<string, object> data)
        {
            var possibleFields = new[] {
                "vehiculo.chasis", "chasis", "CHASIS", "numero_chasis"
            };
            var chassisFull = GetFirstValidValue(data, possibleFields);

            return chassisFull.Replace("CHASIS", "").Replace("chasis", "").Trim();
        }

        #endregion

        #region Datos Financieros

        public decimal ExtractPremium(Dictionary<string, object> data)
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
                    _logger.LogDebug("ExtractPremium - Campo encontrado: '{Field}' = '{Value}'", field, value);
                    var amount = ExtractAmountFromString(value);
                    if (amount > 0)
                    {
                        _logger.LogDebug("ExtractPremium - Premio extraído: {Amount} desde campo '{Field}'", amount, field);
                        return amount;
                    }
                }
            }

            _logger.LogWarning("ExtractPremium - No se encontró valor de premio en ningún campo");
            return 0;
        }

        public decimal ExtractTotalAmount(Dictionary<string, object> data)
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
                    _logger.LogDebug("ExtractTotalAmount - Campo encontrado: '{Field}' = '{Value}'", field, value);

                    if (field == "datos_financiero" && value.Contains("Premio Total a Pagar"))
                    {
                        var match = Regex.Match(value, @"Premio Total a Pagar:\s*\$?\s*([\d.,]+)");
                        if (match.Success)
                        {
                            var amount = ExtractAmountFromString(match.Groups[1].Value);
                            if (amount > 0)
                            {
                                _logger.LogDebug("ExtractTotalAmount - Total extraído: {Amount} desde datos_financiero", amount);
                                return amount;
                            }
                        }
                    }
                    else
                    {
                        var amount = ExtractAmountFromString(value);
                        if (amount > 0)
                        {
                            _logger.LogDebug("ExtractTotalAmount - Total extraído: {Amount} desde campo '{Field}'", amount, field);
                            return amount;
                        }
                    }
                }
            }

            _logger.LogWarning("ExtractTotalAmount - No se encontró valor total en ningún campo");
            return 0;
        }

        public int ExtractInstallmentCount(Dictionary<string, object> data)
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
                    _logger.LogDebug("ExtractInstallmentCount - Campo encontrado: '{Field}' = '{Value}'", field, value);

                    if (field == "cantidadCuotas" || field == "pago.cantidad_cuotas")
                    {
                        if (int.TryParse(value, out var directCuotas) && directCuotas > 0)
                        {
                            _logger.LogDebug("ExtractInstallmentCount - Cuotas desde campo normalizado: {Count}", directCuotas);
                            return directCuotas;
                        }
                    }

                    var match = Regex.Match(value, @"(\d+)\s*cuotas?", RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var cuotas) && cuotas > 0)
                    {
                        _logger.LogDebug("ExtractInstallmentCount - Cuotas extraídas: {Count} desde campo '{Field}'", cuotas, field);
                        return cuotas;
                    }

                    var numberMatch = Regex.Match(value, @"(\d+)");
                    if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out var number) && number > 0 && number <= 60)
                    {
                        _logger.LogDebug("ExtractInstallmentCount - Número extraído: {Count} desde campo '{Field}'", number, field);
                        return number;
                    }
                }
            }

            _logger.LogWarning("ExtractInstallmentCount - No se encontró número de cuotas, usando valor por defecto: 1");
            return 1;
        }

        public string ExtractPaymentMethod(Dictionary<string, object> data)
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

        #endregion

        #region Métodos Auxiliares Privados

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
                        _logger.LogDebug("Monto extraído (formato uruguayo): '{Input}' -> {Amount}", value, amount);
                        return amount;
                    }
                }

                var standardMatch = Regex.Match(cleanValue, @"(\d{1,3}(?:,\d{3})*\.\d{2})");
                if (standardMatch.Success)
                {
                    var standardNumber = standardMatch.Groups[1].Value.Replace(",", "");

                    if (decimal.TryParse(standardNumber, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogDebug("Monto extraído (formato estándar): '{Input}' -> {Amount}", value, amount);
                        return amount;
                    }
                }

                var anyNumberMatch = Regex.Match(cleanValue, @"(\d+(?:[.,]\d+)?)");
                if (anyNumberMatch.Success)
                {
                    var numberStr = anyNumberMatch.Groups[1].Value.Replace(",", ".");

                    if (decimal.TryParse(numberStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    {
                        _logger.LogDebug("Monto extraído (número simple): '{Input}' -> {Amount}", value, amount);
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

            foreach (var kvp in data)
            {
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
                            return candidate;
                        }
                    }
                }
            }

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
                    break;
                }
            }

            if (IsValidPatente(cleaned))
            {
                return cleaned.Replace(" ", "").Replace("-", "").ToUpper();
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

        #endregion
    }
}