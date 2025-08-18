namespace SegurosApp.API.DTOs
{
    public class CriticalFieldsStatus
    {
        public bool HasPolicyNumber { get; set; } = false;
        public bool HasVehicleInfo { get; set; } = false;
        public bool HasDateRange { get; set; } = false;
        public bool HasPremiumInfo { get; set; } = false;

        public List<string> MissingCritical { get; set; } = new();
        public List<string> FoundCritical { get; set; } = new();

        public decimal CriticalFieldsCompleteness =>
            FoundCritical.Count > 0 ?
            (decimal)FoundCritical.Count / (FoundCritical.Count + MissingCritical.Count) * 100 : 0;
    }
}
