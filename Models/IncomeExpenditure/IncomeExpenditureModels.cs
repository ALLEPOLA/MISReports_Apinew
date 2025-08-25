using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MISReports_Api.Models
{
    public class IncomeExpenditureModel
    {
        public string CatFlag { get; set; }
        public string TitleCode { get; set; }
        public string CatCode { get; set; }
        public string AcCd { get; set; }
        public string CatName { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal Clbal { get; set; }
        public decimal Varience { get; set; }
        public string CctName { get; set; }
    }
    public class UserDepartment
    {
        public string DeptId { get; set; }
        public string DeptName { get; set; }
    }
    public class UserCompanyInfo
    {
        public string CompId { get; set; }
        public string CompName { get; set; }
    }

}
