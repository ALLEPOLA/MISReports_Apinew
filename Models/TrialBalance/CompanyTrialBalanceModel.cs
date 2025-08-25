namespace MISReports_Api.Models
{
    public class CompanyTrialBalanceModel
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string TitleFlag { get; set; }
        public string CostCenter { get; set; }
        public string CompanyName { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public class TrialBalanceRequest
    {
        public string CompanyId { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
    }
}