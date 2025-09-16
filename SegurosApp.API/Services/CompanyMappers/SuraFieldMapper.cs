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
    }
}