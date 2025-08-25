namespace MISReports_Api.Models
{
    public class TrialBalanceModel
    {
        public string AcCd { get; set; }
        public string GlName { get; set; }
        public string TitleFlag { get; set; }
        public decimal OpSbal { get; set; }
        public decimal DrSamt { get; set; }
        public decimal CrSamt { get; set; }
        public decimal ClSbal { get; set; }
        public string CctName { get; set; }
    }

    public class RegionDepartment
    {
        public string DeptId { get; set; }
        public string DeptName { get; set; }
    }

    public class CompanyInfo
    {
        public string CompId { get; set; }
        public string CompName { get; set; }
    }

}
