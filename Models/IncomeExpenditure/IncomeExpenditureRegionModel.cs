using System;

namespace MISReports_Api.Models
{
    public class IncomeExpenditureRegionModel
    {
        public string TitleCd { get; set; }
        public string Account { get; set; }
        public decimal Actual { get; set; }
        public string CatName { get; set; }
        public string MaxRev { get; set; }
        public string CatCode { get; set; }
        public string CatFlag { get; set; }
        public string CompName { get; set; }
        public string CostCtr { get; set; }
    }
}
