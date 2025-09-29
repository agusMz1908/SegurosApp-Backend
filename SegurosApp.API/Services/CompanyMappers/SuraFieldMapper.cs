using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services.CompanyMappers
{
    public class SuraFieldMapper : BaseFieldMapper
    {
        public SuraFieldMapper(ILogger<SuraFieldMapper> logger) : base(logger)
        {
        }

        public override string GetCompanyName() => "SURA";

        public override async Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService)
        {
            var normalized = new Dictionary<string, object>(extractedData);
            CleanVehicleFields(normalized);
            MapSuraSpecificFields(normalized);

            return normalized;
        }

        private void MapSuraSpecificFields(Dictionary<string, object> data)
        {
            if (data.ContainsKey("premio.premio") && !data.ContainsKey("poliza.prima_comercial"))
            {
                data["poliza.prima_comercial"] = data["premio.premio"];
                _logger.LogInformation("SURA - Mapeado premio.premio -> poliza.prima_comercial");
            }

            if (data.ContainsKey("premio.total") && !data.ContainsKey("financiero.premio_total"))
            {
                data["financiero.premio_total"] = data["premio.total"];
                _logger.LogInformation("SURA - Mapeado premio.total -> financiero.premio_total");
            }

            if (data.ContainsKey("pago.forma_de_pago"))
            {
                var formaPago = data["pago.forma_de_pago"].ToString();
                var match = Regex.Match(formaPago, @"(\d+)\s*PAGOS?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var cuotas = int.Parse(match.Groups[1].Value);
                    data["pago.cantidad_cuotas"] = cuotas.ToString();
                    data["cantidadCuotas"] = cuotas;
                    _logger.LogInformation("SURA - Extraídas {Cuotas} cuotas desde: {FormaPago}", cuotas, formaPago);
                }
            }

            NormalizeCuotasFromTable(data);
        }

        private void NormalizeCuotasFromTable(Dictionary<string, object> data)
        {
            int cuotasEncontradas = 0;
            for (int i = 1; i <= 12; i++)
            {
                var hasVencimiento = data.ContainsKey($"pago.vencimiento_cuota[{i}]");
                var hasMonto = data.ContainsKey($"pago.prima_cuota[{i}]");

                if (hasVencimiento || hasMonto)
                {
                    cuotasEncontradas++;
                    var bseIndex = i - 1;

                    if (hasVencimiento)
                    {
                        var fecha = data[$"pago.vencimiento_cuota[{i}]"].ToString();
                        data[$"pago.cuotas[{bseIndex}].vencimiento"] = $"Vencimiento:\n{fecha}";
                    }

                    if (hasMonto)
                    {
                        var monto = data[$"pago.prima_cuota[{i}]"].ToString();
                        data[$"pago.cuotas[{bseIndex}].prima"] = $"Prima:\n$ {monto}";
                    }
                }
            }

            if (cuotasEncontradas > 0)
            {
                data["pago.cantidad_cuotas"] = cuotasEncontradas.ToString();
                data["cantidadCuotas"] = cuotasEncontradas;
                _logger.LogInformation("SURA - Normalizadas {Count} cuotas desde tabla al formato BSE", cuotasEncontradas);
            }
        }

        private void CleanVehicleFields(Dictionary<string, object> data)
        {
            var fieldsToClean = new Dictionary<string, string[]>
            {
                ["vehiculo.marca"] = new[] { "Marca\n", "Marca ", "MARCA\n", "MARCA " },
                ["vehiculo.modelo"] = new[] { "Modelo\n", "Modelo ", "MODELO\n", "MODELO " },
                ["vehiculo.motor"] = new[] { "Motor\n", "Motor ", "MOTOR\n", "MOTOR ", "motor\n", "motor " },
                ["vehiculo.chasis"] = new[] { "Chasis\n", "Chasis ", "CHASIS\n", "CHASIS ", "chasis\n", "chasis " },
                ["vehiculo.anio"] = new[] { "Año\n", "Año ", "AÑO\n", "AÑO ", "año\n", "año " },
                ["vehiculo.color"] = new[] { "Color\n", "Color ", "COLOR\n", "COLOR " },
                ["vehiculo.tipo"] = new[] { "Tipo\n", "Tipo ", "TIPO\n", "TIPO " },
                ["vehiculo.matricula"] = new[] { "Matrícula\n", "Matrícula ", "MATRÍCULA\n", "MATRÍCULA ", "Matricula\n", "Matricula ", "MATRICULA\n", "MATRICULA " },
                ["vehiculo.patente"] = new[] { "Patente\n", "Patente ", "PATENTE\n", "PATENTE " }
            };

            foreach (var fieldConfig in fieldsToClean)
            {
                var fieldName = fieldConfig.Key;
                var prefixes = fieldConfig.Value;

                if (data.ContainsKey(fieldName))
                {
                    var originalValue = data[fieldName]?.ToString() ?? "";
                    var cleanedValue = CleanFieldValue(originalValue, prefixes);

                    if (cleanedValue != originalValue)
                    {
                        data[fieldName] = cleanedValue;
                        _logger.LogInformation("SURA - Campo limpiado: {Field} '{Original}' -> '{Clean}'",
                            fieldName, originalValue, cleanedValue);
                    }
                }
            }
        }

        private string CleanFieldValue(string value, string[] prefixesToRemove)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var cleaned = value.Replace("\r\n", " ")
                              .Replace("\n", " ")
                              .Replace("\r", "")
                              .Trim();

            foreach (var prefix in prefixesToRemove)
            {
                if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(prefix.Length).Trim();
                    break;
                }
            }

            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }

            return cleaned;
        }
    }
}