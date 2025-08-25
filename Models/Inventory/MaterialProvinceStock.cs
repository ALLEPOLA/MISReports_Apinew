namespace MISReports_Api.Models
{
    public class MaterialProvinceStock
    {
        public string Province { get; set; }
        public string MatCd { get; set; }
        public decimal QtyOnHand { get; set; }
        public string ErrorMessage { get; set; }
    }
}
