using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Services.CompanyMappers;

namespace SegurosApp.API.Services.CompanyMappers
{
    public class MapfreFieldMapper : BSEFieldMapper
    {
        public MapfreFieldMapper(ILogger<MapfreFieldMapper> logger) : base(logger)
        {
        }

        public override string GetCompanyName() => "MAPFRE";

        public override async Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService)
        {
            var normalized = await base.NormalizeFieldsAsync(extractedData, masterDataService);

            // Aplicar normalizaciones específicas de MAPFRE
            MapMapfreSpecificFields(normalized);

            return normalized;
        }

        private void MapMapfreSpecificFields(Dictionary<string, object> data)
        {
            // Limpiar campos de vehículo con prefijos
            CleanVehicleFields(data);

            // Mapear costo como prima comercial
            if (data.ContainsKey("costo.costo") && !data.ContainsKey("poliza.prima_comercial"))
            {
                data["poliza.prima_comercial"] = data["costo.costo"];
                _logger.LogInformation("MAPFRE - Mapeado costo.costo -> poliza.prima_comercial");
            }

            // Mapear premio total
            if (data.ContainsKey("costo.premio_total") && !data.ContainsKey("financiero.premio_total"))
            {
                data["financiero.premio_total"] = data["costo.premio_total"];
                _logger.LogInformation("MAPFRE - Mapeado costo.premio_total -> financiero.premio_total");
            }

            // Contar cuotas MAPFRE
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

                // Normalizar modalidades conocidas de MAPFRE
                var modalidadNormalizada = NormalizeModalidad(modalidad);
                if (modalidadNormalizada != modalidad)
                {
                    data["poliza.modalidad_normalizada"] = modalidadNormalizada;
                    _logger.LogInformation("MAPFRE - Modalidad normalizada: '{Original}' -> '{Normalizada}'",
                        modalidad, modalidadNormalizada);
                }
            }
        }

        private string NormalizeModalidad(string modalidad)
        {
            if (string.IsNullOrWhiteSpace(modalidad)) return modalidad;

            var upper = modalidad.ToUpper();

            // Ser más específico con TODO RIESGO
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
            var fieldsToClean = new[]
            {
                "vehiculo.marca",
                "vehiculo.modelo",
                "vehiculo.motor",
                "vehiculo.chasis",
                "vehiculo.anio"
            };

            foreach (var field in fieldsToClean)
            {
                if (data.ContainsKey(field))
                {
                    var originalValue = data[field].ToString();
                    var cleanedValue = CleanFieldValue(originalValue, field);

                    if (cleanedValue != originalValue)
                    {
                        data[field] = cleanedValue;
                        _logger.LogInformation("MAPFRE - Campo limpiado: {Field} '{Original}' -> '{Clean}'",
                            field, originalValue, cleanedValue);
                    }
                }
            }
        }

        private string CleanFieldValue(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var cleaned = value.Replace("\n", " ").Replace("\r", "").Trim();

            // Prefijos específicos de MAPFRE
            var prefixesToRemove = new Dictionary<string, string[]>
            {
                ["vehiculo.marca"] = new[] { "Marca\n", "Marca ", "MARCA\n", "MARCA " },
                ["vehiculo.modelo"] = new[] { "Modelo\n", "Modelo ", "MODELO\n", "MODELO " },
                ["vehiculo.motor"] = new[] { "Motor\n", "Motor ", "MOTOR\n", "MOTOR " },
                ["vehiculo.chasis"] = new[] { "Chasis\n", "Chasis ", "CHASIS\n", "CHASIS " },
                ["vehiculo.anio"] = new[] { "Año\n", "Año ", "AÑO\n", "AÑO " }
            };

            if (prefixesToRemove.ContainsKey(fieldName))
            {
                foreach (var prefix in prefixesToRemove[fieldName])
                {
                    if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        cleaned = cleaned.Substring(prefix.Length).Trim();
                        break;
                    }
                }
            }

            return cleaned;
        }
    }
}