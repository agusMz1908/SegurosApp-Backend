using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;

namespace SegurosApp.API.Services.CompanyMappers
{
    public class BSEFieldMapper : BaseFieldMapper
    {
        public BSEFieldMapper(ILogger<BSEFieldMapper> logger) : base(logger)
        {
        }

        public override string GetCompanyName() => "BSE";

        public override async Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService)
        {
            _logger.LogDebug("Normalizando campos BSE");
            var normalized = new Dictionary<string, object>(extractedData);

            CleanVehicleFields(normalized);
            MapStandardBSEFields(normalized);

            return await Task.FromResult(normalized);
        }

        private void CleanVehicleFields(Dictionary<string, object> data)
        {
            var fieldsToClean = new Dictionary<string, string[]>
            {
                ["vehiculo.marca"] = new[] { "MARCA\n", "MARCA ", "MARCA:", "Marca\n", "Marca ", "Marca:" },
                ["vehiculo.modelo"] = new[] { "MODELO\n", "MODELO ", "MODELO:", "Modelo\n", "Modelo ", "Modelo:" },
                ["vehiculo.motor"] = new[] { "MOTOR\n", "MOTOR ", "MOTOR:", "Motor\n", "Motor ", "Motor:" },
                ["vehiculo.chasis"] = new[] { "CHASIS\n", "CHASIS ", "CHASIS:", "Chasis\n", "Chasis ", "Chasis:" },
                ["vehiculo.anio"] = new[] { "AÑO\n", "AÑO ", "AÑO:", "Año\n", "Año ", "Año:" },
                ["vehiculo.patente"] = new[] { "MATRÍCULA\n", "MATRÍCULA ", "PATENTE\n", "PATENTE ", "Matrícula\n", "Patente\n" }
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
                        _logger.LogInformation("BSE - Campo limpiado: {Field} '{Original}' -> '{Clean}'",
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

        private void MapStandardBSEFields(Dictionary<string, object> data)
        {
            _logger.LogDebug("Campos BSE mapeados (formato estándar)");
        }
    }
}