using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;
using System.Text.RegularExpressions;

namespace SegurosApp.API.Services.CompanyMappers
{
    public abstract class BaseFieldMapper : ICompanyFieldMapper
    {
        protected readonly ILogger _logger;

        protected BaseFieldMapper(ILogger logger)
        {
            _logger = logger;
        }

        public abstract string GetCompanyName();

        public abstract Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService);

        #region Métodos comunes de mapeo (shared por todas las compañías)

        public virtual async Task<int> MapCombustibleAsync(Dictionary<string, object> data, List<CombustibleItem> combustibles)
        {
            var combustibleText = GetFieldValue(data, "vehiculo.combustible", "COMBUSTIBLE");
            var combustibleId = MapCombustibleByText(combustibleText, combustibles);
            return int.TryParse(combustibleId, out var id) ? id : 1;
        }

        public virtual async Task<int> MapDestinoAsync(Dictionary<string, object> data, List<DestinoItem> destinos)
        {
            var destinoText = GetFieldValue(data, "vehiculo.destino_del_vehiculo", "DESTINO DEL VEHÍCULO");
            return MapDestinoByText(destinoText, destinos);
        }

        public virtual async Task<int> MapDepartamentoAsync(Dictionary<string, object> data, List<DepartamentoItem> departamentos)
        {
            var deptoText = GetFieldValue(data, "asegurado.departamento", "asegurado.localidad");
            return MapDepartamentoByText(deptoText, departamentos);
        }

        public virtual async Task<int> MapCalidadAsync(Dictionary<string, object> data, List<CalidadItem> calidades)
        {
            var calidadText = GetFieldValue(data, "vehiculo.calidad_de_contratante", "CALIDAD DE CONTRATANTE");
            return MapCalidadByText(calidadText, calidades);
        }

        public virtual async Task<int> MapCategoriaAsync(Dictionary<string, object> data, List<CategoriaItem> categorias)
        {
            var categoriaText = GetFieldValue(data, "vehiculo.tipo_de_vehiculo", "vehiculo.tipo");
            return MapCategoriaByText(categoriaText, categorias);
        }

        public virtual async Task<int> MapTarifaAsync(Dictionary<string, object> data, List<TarifaItem> tarifas)
        {
            var modalidadText = GetFieldValue(data, "poliza.modalidad", "vehiculo.modalidad");
            return MapTarifaByText(modalidadText, tarifas);
        }

        #endregion

        #region Métodos auxiliares protegidos (compartidos)

        protected string GetFieldValue(Dictionary<string, object> data, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (data.TryGetValue(key, out var value) && value != null)
                {
                    var stringValue = value.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        return stringValue;
                    }
                }
            }
            return "";
        }

        protected string MapCombustibleByText(string combustibleText, List<CombustibleItem> combustibles)
        {
            if (string.IsNullOrEmpty(combustibleText))
                return "1";

            var upper = combustibleText.ToUpperInvariant();

            if (upper.Contains("GASOIL") || upper.Contains("DIESEL"))
                return combustibles.FirstOrDefault(c => c.name.Contains("GASOIL"))?.id ?? "2";

            if (upper.Contains("GASOLINA") || upper.Contains("NAFTA"))
                return combustibles.FirstOrDefault(c => c.name.Contains("GASOLINA"))?.id ?? "1";

            if (upper.Contains("ELECTRICO") || upper.Contains("ELÉCTRICO"))
                return combustibles.FirstOrDefault(c => c.name.Contains("ELECTRICO"))?.id ?? "3";

            if (upper.Contains("HIBRIDO") || upper.Contains("HÍBRIDO"))
                return combustibles.FirstOrDefault(c => c.name.Contains("HIBRIDO"))?.id ?? "4";

            return "1";
        }

        protected int MapDestinoByText(string destinoText, List<DestinoItem> destinos)
        {
            if (string.IsNullOrEmpty(destinoText))
                return 1;

            var upper = destinoText.ToUpperInvariant();

            if (upper.Contains("PARTICULAR"))
                return destinos.FirstOrDefault(d => d.desnom.Contains("PARTICULAR"))?.id ?? 1;

            if (upper.Contains("COMERCIAL") || upper.Contains("TRABAJO"))
                return destinos.FirstOrDefault(d => d.desnom.Contains("COMERCIAL"))?.id ?? 2;

            return 1;
        }

        protected int MapDepartamentoByText(string deptoText, List<DepartamentoItem> departamentos)
        {
            if (string.IsNullOrEmpty(deptoText))
                return 1;

            var cleanText = deptoText.ToUpperInvariant()
                .Replace("DEPARTAMENTO", "")
                .Replace("DPTO", "")
                .Trim();

            var match = departamentos.FirstOrDefault(d =>
                d.dptnom.Equals(cleanText, StringComparison.OrdinalIgnoreCase) ||
                cleanText.Contains(d.dptnom.ToUpperInvariant()));

            return match?.id ?? 1;
        }

        protected int MapCalidadByText(string calidadText, List<CalidadItem> calidades)
        {
            if (string.IsNullOrEmpty(calidadText))
                return 1;

            var upper = calidadText.ToUpperInvariant();

            if (upper.Contains("PROPIETARIO"))
                return calidades.FirstOrDefault(c => c.caldsc.Contains("PROPIETARIO"))?.id ?? 1;

            if (upper.Contains("ARRENDATARIO"))
                return calidades.FirstOrDefault(c => c.caldsc.Contains("ARRENDATARIO"))?.id ?? 2;

            return 1;
        }

        protected int MapCategoriaByText(string categoriaText, List<CategoriaItem> categorias)
        {
            if (string.IsNullOrEmpty(categoriaText))
                return 1;

            var upper = categoriaText.ToUpperInvariant();

            if (upper.Contains("AUTOMOVIL") || upper.Contains("AUTO"))
                return categorias.FirstOrDefault(c => c.catdsc.Contains("AUTOMOVIL"))?.id ?? 1;

            if (upper.Contains("CAMIONETA") || upper.Contains("PICK"))
                return categorias.FirstOrDefault(c => c.catdsc.Contains("CAMIONETA"))?.id ?? 2;

            if (upper.Contains("MOTO"))
                return categorias.FirstOrDefault(c => c.catdsc.Contains("MOTO"))?.id ?? 3;

            return 1;
        }

        protected int MapTarifaByText(string modalidadText, List<TarifaItem> tarifas)
        {
            if (string.IsNullOrEmpty(modalidadText))
                return 1;

            var upper = modalidadText.ToUpperInvariant();

            if (upper.Contains("TODO RIESGO"))
                return tarifas.FirstOrDefault(t => t.tarnom.Contains("TODO RIESGO"))?.id ?? 1;

            if (upper.Contains("TERCEROS"))
                return tarifas.FirstOrDefault(t => t.tarnom.Contains("TERCEROS"))?.id ?? 2;

            return 1;
        }

        protected string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            return input
                .Trim()
                .Replace("  ", " ")
                .Replace("\n", " ")
                .Replace("\r", "")
                .Replace("\t", " ");
        }

        protected bool TryGetValue(Dictionary<string, object> data, string key, out string value)
        {
            value = "";
            if (data.TryGetValue(key, out var obj) && obj != null)
            {
                value = obj.ToString()?.Trim() ?? "";
                return !string.IsNullOrEmpty(value);
            }
            return false;
        }

        #endregion
    }
}