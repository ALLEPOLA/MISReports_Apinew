using System;

namespace MISReports_Api.Models
{
    public class MaterialStockBalance
    {
        public string MatCd { get; set; }
        public string Region { get; set; }
        public string Province { get; set; }
        public string DeptId { get; set; }
        public string MatNm { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CommittedCost { get; set; }
        public decimal ReorderQty { get; set; }  
        public string UomCd { get; set; }
        public string ErrorMessage { get; set; }
    }
}
