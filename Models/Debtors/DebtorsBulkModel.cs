using System;

namespace MISReports_Api.Models
{
    public class DebtorsBulkModel
    {
        public string Type { get; set; }
        public string CustType { get; set; }
        public decimal TotDebtors { get; set; }
        public decimal Month01 { get; set; }
        public decimal Month02 { get; set; }
        public decimal Month03 { get; set; }
        public decimal Month04 { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DebtorsBulkRequest
    {
        public string Opt { get; set; }
        public string Cycle { get; set; }
        public string AreaCode { get; set; }
    }
}