using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class DebtorDetailModel
    {
        public string AreaName { get; set; }
        public string AccountNumber { get; set; }
        public string TariffCode { get; set; }
        public decimal OutstandingBalance { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }

        // Age buckets
        public decimal Month0 { get; set; }
        public decimal Month1 { get; set; }
        public decimal Month2 { get; set; }
        public decimal Month3 { get; set; }
        public decimal Month4 { get; set; }
        public decimal Month5 { get; set; }
        public decimal Month6 { get; set; }
        public decimal Months7_9 { get; set; }
        public decimal Months10_12 { get; set; }
        public decimal Months13_24 { get; set; }
        public decimal Months25_36 { get; set; }
        public decimal Months37_48 { get; set; }
        public decimal Months49_60 { get; set; }
        public decimal Months61Plus { get; set; }

        public string ErrorMessage { get; set; }
    }

    public enum AgeRange
    {
        Months0_6,
        Months7_12,
        Years1_2,
        Years2_3,
        Years3_4,
        Years4_5,
        Years5Plus,
        All
    }

    public class DebtorRequest
    {
        public string CustType { get; set; }
        public string BillCycle { get; set; }
        public string AreaCode { get; set; }
        public AgeRange AgeRange { get; set; } = AgeRange.All;
    }
}