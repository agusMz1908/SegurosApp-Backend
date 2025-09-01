namespace SegurosApp.API.DTOs
{
    public class AzureModelInfo
    {
        public string ModelId { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string CompaniaAlias { get; set; } = "";
        public int CompaniaId { get; set; }
        public string Description { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}