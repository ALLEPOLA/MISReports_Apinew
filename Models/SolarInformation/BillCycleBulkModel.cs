using System.Collections.Generic;

namespace MISReports_Api.Models.SolarInformation
{
    public class BillCycleBulkModel
    {
        public string MaxBillCycle { get; set; }
        public List<string> BillCycles { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }
}