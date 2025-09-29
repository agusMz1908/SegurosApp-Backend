using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;

namespace SegurosApp.API.Services.CompanyMappers
{
    public class MapfreFieldMapper : BaseFieldMapper
    {
        public MapfreFieldMapper(ILogger<MapfreFieldMapper> logger) : base(logger)
        {
        }

        public override string GetCompanyName() => "MAPFRE";

        public override async Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService)
        {
            var normalized = new Dictionary<string, object>(extractedData);

            CleanVehicleFields(normalized);
            MapMapfreSpecificFields(normalized);
            NormalizeCuotasToBSEFormat(normalized);

            return normalized;
        }

        private void MapMapfreSpecificFields(Dictionary<string, object> data)
        {
            if (data.ContainsKey("costo.costo") && !data.ContainsKey("poliza.prima_comercial"))
            {
                data["poliza.prima_comercial"] = data["costo.costo"];
                _logger.LogInformation("MAPFRE - Mapeado costo.costo -> poliza.prima_comercial");
            }

            if (data.ContainsKey("costo.premio_total") && !data.ContainsKey("financiero.premio_total"))
            {
                data["financiero.premio_total"] = data["costo.premio_total"];
                _logger.LogInformation("MAPFRE - Mapeado costo.premio_total -> financiero.premio_total");
            }

            int cuotasEncontradas = 0;
            for (int i = 1; i <= 12; i++)
            {
                if (data.ContainsKey($"pago.vencimiento_cuota[{i}]"))
                {
                    cuotasEncontradas++;
                }
            }

            if (cuotasEncontradas > 0)
            {
                data["pago.cantidad_cuotas"] = cuotasEncontradas.ToString();
                data["cantidadCuotas"] = cuotasEncontradas;
                _logger.LogInformation("MAPFRE - Detectadas {Cuotas} cuotas", cuotasEncontradas);
            }

            if (data.ContainsKey("poliza.modalidad"))
            {
                var modalidad = data["poliza.modalidad"].ToString();
                var modalidadNormalizada = NormalizeModalidad(modalidad);
                if (modalidadNormalizada != modalidad)
                {
                    data["poliza.modalidad_normalizada"] = modalidadNormalizada;
                    _logger.LogInformation("MAPFRE - Modalidad normalizada: '{Original}' -> '{Normalizada}'",
                        modalidad, modalidadNormalizada);
                }
            }
        }

        private void NormalizeCuotasToBSEFormat(Dictionary<string, object> data)
        {
            _logger.LogInformation("MAPFRE - Normalizando cuotas al formato BSE");

            int cuotasCount = 0;
            if (data.TryGetValue("cantidadCuotas", out var cuotasObj) && int.TryParse(cuotasObj.ToString(), out var count))
            {
                cuotasCount = count;
            }

            for (int i = 1; i <= 12; i++)
            {
                var bseIndex = i - 1; 

                if (data.ContainsKey($"pago.vencimiento_cuota[{i}]"))
                {
                    var fecha = data[$"pago.vencimiento_cuota[{i}]"].ToString();
                    data[$"pago.cuotas[{bseIndex}].vencimiento"] = $"Vencimiento:\n{fecha}";
                    _logger.LogDebug("MAPFRE - Cuota {Index}: Fecha convertida a formato BSE", i);
                }

                if (data.ContainsKey($"pago.cuota_monto[{i}]"))
                {
                    var monto = data[$"pago.cuota_monto[{i}]"].ToString();
                    data[$"pago.cuotas[{bseIndex}].prima"] = $"Prima:\n$ {monto}";
                    _logger.LogDebug("MAPFRE - Cuota {Index}: Monto convertido a formato BSE", i);
                }
            }

            if (cuotasCount > 0)
            {
                _logger.LogInformation("MAPFRE - Normalizadas {Count} cuotas al formato BSE", cuotasCount);
            }
        }

        private string NormalizeModalidad(string modalidad)
        {
            if (string.IsNullOrWhiteSpace(modalidad)) return modalidad;

            var upper = modalidad.ToUpper();

            if (upper.Contains("TODO RIESGO") && upper.Contains("TOTAL"))
                return "TODO RIESGO TOTAL";

            if (upper.Contains("TODO RIESGO"))
                return "TODO RIESGO";

            if (upper.Contains("TOTAL") && !upper.Contains("BASICO"))
                return "TOTAL";

            if (upper.Contains("TERCEROS") || upper.Contains("RC"))
                return "TERCEROS";

            if (upper.Contains("BASICA") || upper.Contains("MINIMA"))
                return "BASICA";

            return modalidad;
        }

        private void CleanVehicleFields(Dictionary<string, object> data)
        {
            var fieldsToClean = new Dictionary<string, string[]>
            {
                ["vehiculo.marca"] = new[] { "Marca\n", "Marca ", "MARCA\n", "MARCA " },
                ["vehiculo.modelo"] = new[] { "Modelo\n", "Modelo ", "MODELO\n", "MODELO " },
                ["vehiculo.motor"] = new[] { "Motor\n", "Motor ", "MOTOR\n", "MOTOR ", "motor\n", "motor " },
                ["vehiculo.chasis"] = new[] { "Chasis\n", "Chasis ", "CHASIS\n", "CHASIS ", "chasis\n", "chasis " },
                ["vehiculo.anio"] = new[] { "Año\n", "Año ", "AÑO\n", "AÑO ", "año\n", "año " }
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
                        _logger.LogInformation("MAPFRE - Campo limpiado: {Field} '{Original}' -> '{Clean}'",
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