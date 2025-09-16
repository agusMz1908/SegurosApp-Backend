using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;

namespace SegurosApp.API.Services.CompanyMappers
{
    /// <summary>
    /// Interface que define el contrato para mapear campos específicos por compañía.
    /// Cada compañía implementa esta interface con su lógica específica de mapeo.
    /// </summary>
    public interface ICompanyFieldMapper
    {
        /// <summary>
        /// Normaliza los campos extraídos por Azure al formato estándar interno
        /// </summary>
        Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService);

        /// <summary>
        /// Mapea el combustible del vehículo
        /// </summary>
        Task<int> MapCombustibleAsync(Dictionary<string, object> data, List<CombustibleItem> combustibles);

        /// <summary>
        /// Mapea el destino del vehículo
        /// </summary>
        Task<int> MapDestinoAsync(Dictionary<string, object> data, List<DestinoItem> destinos);

        /// <summary>
        /// Mapea el departamento del asegurado
        /// </summary>
        Task<int> MapDepartamentoAsync(Dictionary<string, object> data, List<DepartamentoItem> departamentos);

        /// <summary>
        /// Mapea la calidad del contratante
        /// </summary>
        Task<int> MapCalidadAsync(Dictionary<string, object> data, List<CalidadItem> calidades);

        /// <summary>
        /// Mapea la categoría del vehículo
        /// </summary>
        Task<int> MapCategoriaAsync(Dictionary<string, object> data, List<CategoriaItem> categorias);

        /// <summary>
        /// Mapea la tarifa/modalidad de la póliza
        /// </summary>
        Task<int> MapTarifaAsync(Dictionary<string, object> data, List<TarifaItem> tarifas);

        /// <summary>
        /// Retorna el nombre de la compañía que maneja este mapper
        /// </summary>
        string GetCompanyName();
    }

    /// <summary>
    /// Interface auxiliar para uniformizar items de Velneo
    /// </summary>
    public interface IVelneoMasterItem
    {
        int Id { get; }
        string? Nombre { get; }
        string? Codigo { get; }
    }

    /// <summary>
    /// Extensiones para convertir items específicos de Velneo a la interface común
    /// </summary>
    public static class VelneoItemExtensions
    {
        public static IVelneoMasterItem ToMasterItem(this CombustibleItem item) =>
            new VelneoMasterItemWrapper(0, item.name, null);

        public static IVelneoMasterItem ToMasterItem(this DestinoItem item) =>
            new VelneoMasterItemWrapper(item.id, item.desnom, item.descod);

        public static IVelneoMasterItem ToMasterItem(this DepartamentoItem item) =>
            new VelneoMasterItemWrapper(item.id, item.dptnom, item.sc_cod);

        public static IVelneoMasterItem ToMasterItem(this CalidadItem item) =>
            new VelneoMasterItemWrapper(item.id, item.caldsc, item.calcod);

        public static IVelneoMasterItem ToMasterItem(this CategoriaItem item) =>
            new VelneoMasterItemWrapper(item.id, item.catdsc, item.catcod);

        public static IVelneoMasterItem ToMasterItem(this TarifaItem item) =>
            new VelneoMasterItemWrapper(item.id, item.tarnom, item.tarcod);

        private class VelneoMasterItemWrapper : IVelneoMasterItem
        {
            public int Id { get; }
            public string? Nombre { get; }
            public string? Codigo { get; }

            public VelneoMasterItemWrapper(int id, string? nombre, string? codigo)
            {
                Id = id;
                Nombre = nombre;
                Codigo = codigo;
            }
        }
    }
}