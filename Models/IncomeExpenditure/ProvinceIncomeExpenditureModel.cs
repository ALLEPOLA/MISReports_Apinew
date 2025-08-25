using System;

namespace MISReports_Api.Models
{
    public class ProvinceIncomeExpenditureModel
    {
        public string TitleCd { get; set; }
        public string Account { get; set; }
        public decimal Actual { get; set; }
        public string CatName { get; set; }
        public string MaxRev { get; set; }
        public string CatCode { get; set; }
        public string CatFlag { get; set; }
        public string AreaNum { get; set; }
        public string CctName { get; set; }

        // Optional: Add some computed properties or validation
        public string FormattedActual => Actual.ToString("N2");
        public bool IsIncome => TitleCd?.StartsWith("IN") == true;
        public bool IsExpenditure => TitleCd?.StartsWith("XP") == true;
    }
}