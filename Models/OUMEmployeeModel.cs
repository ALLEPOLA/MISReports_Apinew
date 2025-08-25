using System;

namespace MISReports_Api.Models
{
    public class OUMEmployeeModel
    {
        public DateTime AuthDate { get; set; }
        public int OrderId { get; set; }
        public string AcctNumber { get; set; }
        public string BankCode { get; set; }
        public decimal BillAmt { get; set; }
        public decimal TaxAmt { get; set; }
        public decimal TotAmt { get; set; }
        public string AuthCode { get; set; }
        public string CardNo { get; set; }
    }
}
