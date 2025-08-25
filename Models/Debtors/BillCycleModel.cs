// Models/BillCycleModel.cs
using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class BillCycleModel
    {
        public string MaxBillCycle { get; set; }
        public List<string> BillCycles { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }

    public class CustomerTypeModel
    {
        public string CustType { get; set; }
        public string Description { get; set; }
        public string ErrorMessage { get; set; }
    }
}