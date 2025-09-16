using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;
using SegurosApp.API.Services.CompanyMappers;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services.CompanyMappers
{
    public class SuraFieldMapper : BSEFieldMapper
    {
        public SuraFieldMapper(ILogger<SuraFieldMapper> logger) : base(logger)
        {
        }

        public override string GetCompanyName() => "SURA";

        public override async Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService)
        {
            var normalized = await base.NormalizeFieldsAsync(extractedData, masterDataService);

            // Mapear campos específicos de SURA
            MapSuraSpecificFields(normalized);

            // ✅ NUEVO: Limpiar campos del vehículo (quitar prefijos)
            CleanVehicleFields(normalized);

            return normalized;
        }

        private void MapSuraSpecificFields(Dictionary<string, object> data)
        {
            // Mapear premio de SURA
            if (data.ContainsKey("premio.premio") && !data.ContainsKey("poliza.prima_comercial"))
            {
                data["poliza.prima_comercial"] = data["premio.premio"];
                _logger.LogInformation("SURA - Mapeado premio.premio -> poliza.prima_comercial");
            }

            // Mapear total de SURA
            if (data.ContainsKey("premio.total") && !data.ContainsKey("financiero.premio_total"))
            {
                data["financiero.premio_total"] = data["premio.total"];
                _logger.LogInformation("SURA - Mapeado premio.total -> financiero.premio_total");
            }

            // Mapear cuotas de SURA - extraer número de "10 PAGOS"
            if (data.ContainsKey("pago.forma_de_pago"))
            {
                var formaPago = data["pago.forma_de_pago"].ToString();
                var match = Regex.Match(formaPago, @"(\d+)\s*PAGOS", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var cuotas = int.Parse(match.Groups[1].Value);
                    data["pago.cantidad_cuotas"] = cuotas.ToString();
                    data["cantidadCuotas"] = cuotas;
                    _logger.LogInformation("SURA - Extraídas {Cuotas} cuotas desde: {FormaPago}", cuotas, formaPago);
                }
            }
        }

        // ✅ NUEVO: Método para limpiar campos del vehículo
        private void CleanVehicleFields(Dictionary<string, object> data)
        {
            var fieldsToClean = new[]
            {
                "vehiculo.marca",
                "vehiculo.modelo",
                "vehiculo.motor",
                "vehiculo.chasis",
                "vehiculo.anio",
                "vehiculo.color",
                "vehiculo.tipo",
                "vehiculo.matricula",
                "vehiculo.patente"
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
                        _logger.LogInformation("SURA - Campo limpiado: {Field} '{Original}' -> '{Clean}'",
                            field, originalValue, cleanedValue);
                    }
                }
            }
        }

        // ✅ NUEVO: Método para limpiar valores específicos de SURA
        private string CleanFieldValue(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var cleaned = value.Replace("\n", " ").Replace("\r", "").Trim();

            // Prefijos específicos de SURA
            var prefixesToRemove = new Dictionary<string, string[]>
            {
                ["vehiculo.marca"] = new[] { "Marca\n", "Marca ", "MARCA\n", "MARCA " },
                ["vehiculo.modelo"] = new[] { "Modelo\n", "Modelo ", "MODELO\n", "MODELO " },
                ["vehiculo.motor"] = new[] { "Motor\n", "Motor ", "MOTOR\n", "MOTOR " },
                ["vehiculo.chasis"] = new[] { "Chasis\n", "Chasis ", "CHASIS\n", "CHASIS " },
                ["vehiculo.anio"] = new[] { "Año\n", "Año ", "AÑO\n", "AÑO " },
                ["vehiculo.color"] = new[] { "Color\n", "Color ", "COLOR\n", "COLOR " },
                ["vehiculo.tipo"] = new[] { "Tipo\n", "Tipo ", "TIPO\n", "TIPO " },
                ["vehiculo.matricula"] = new[] { "Matrícula\n", "Matrícula ", "MATRÍCULA\n", "MATRÍCULA ", "Matricula\n", "Matricula ", "MATRICULA\n", "MATRICULA " },
                ["vehiculo.patente"] = new[] { "Patente\n", "Patente ", "PATENTE\n", "PATENTE " }
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