namespace SegurosApp.API.DTOs.Velneo.Request
{
    public class CreatePolizaRequest
    {
        public int ScanId { get; set; }
        public int ClientId { get; set; }
        public int BrokerId { get; set; }
        public int CompanyId { get; set; }
        public int SectionId { get; set; } 
        public int DepartmentId { get; set; }
        public string FuelId { get; set; } = "";
        public int DestinationId { get; set; }
        public int CategoryId { get; set; }
        public int QualityId { get; set; }
        public int TariffId { get; set; }
        public string PolicyNumber { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public decimal Premium { get; set; } = 0;
        public string PaymentMethod { get; set; } = "";
        public int InstallmentCount { get; set; } = 1;
        public string VehicleBrand { get; set; } = "";
        public string VehicleModel { get; set; } = "";
        public int VehicleYear { get; set; } = 0;
        public string MotorNumber { get; set; } = "";
        public string ChassisNumber { get; set; } = "";
        public string Notes { get; set; } = "";
        public List<string> CorrectedFields { get; set; } = new();
    }
}
