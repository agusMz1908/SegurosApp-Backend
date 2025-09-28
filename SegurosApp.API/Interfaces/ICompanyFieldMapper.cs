using SegurosApp.API.DTOs.Velneo.Item;
using SegurosApp.API.Interfaces;

namespace SegurosApp.API.Services.CompanyMappers
{
    public interface ICompanyFieldMapper
    {
        Task<Dictionary<string, object>> NormalizeFieldsAsync(
            Dictionary<string, object> extractedData,
            IVelneoMasterDataService masterDataService);

        Task<int> MapCombustibleAsync(Dictionary<string, object> data, List<CombustibleItem> combustibles);
        Task<int> MapDestinoAsync(Dictionary<string, object> data, List<DestinoItem> destinos);
        Task<int> MapDepartamentoAsync(Dictionary<string, object> data, List<DepartamentoItem> departamentos);
        Task<int> MapCalidadAsync(Dictionary<string, object> data, List<CalidadItem> calidades);
        Task<int> MapCategoriaAsync(Dictionary<string, object> data, List<CategoriaItem> categorias);
        Task<int> MapTarifaAsync(Dictionary<string, object> data, List<TarifaItem> tarifas);
        string GetCompanyName();
    }

    public interface IVelneoMasterItem
    {
        int Id { get; }
        string? Nombre { get; }
        string? Codigo { get; }
    }

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