namespace MISReports_Api.Models.SolarInformation
{
    public class SolarProgressDetailedModel
    {
        public string Region { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }
        public string AccountNumber { get; set; }
        public string NetType { get; set; }
        public string Description { get; set; }
        public decimal Capacity { get; set; }
        public string FromArea { get; set; }
        public string ToArea { get; set; }
        public string FromNetType { get; set; }
        public string ToNetType { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarProgressRequest
    {
        public string BillCycle { get; set; }
        public SolarReportType ReportType { get; set; }
        public string AreaCode { get; set; }
        public string ProvCode { get; set; }
        public string Region { get; set; }
    }

    public enum SolarReportType
    {
        Area,
        Province,
        Region,
        EntireCEB
    }
}