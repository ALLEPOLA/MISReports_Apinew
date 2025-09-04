using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models.SolarInformation
{
    public class SolarProgressSummaryModel
    {
        public string Region { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }
        public string Description { get; set; }
        public int Count { get; set; }
        public decimal Capacity { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarProgressSummaryRequest
    {
        public string BillCycle { get; set; }
        public SolarReportType ReportType { get; set; }
        public string AreaCode { get; set; }
        public string ProvCode { get; set; }
        public string Region { get; set; }
    }

    public enum SolarSummaryReportType
    {
        Area,
        Province,
        Region,
        EntireCEB
    }
}