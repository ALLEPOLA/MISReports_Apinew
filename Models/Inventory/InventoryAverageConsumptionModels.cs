using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class Warehouse
    {
        public string WarehouseCode { get; set; }
    }

    public class InventoryAverageConsumption
    {
        public string WarehouseCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string GradeCode { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal TransactionQuantity { get; set; }
        public decimal AverageConsumption { get; set; }
        public string CostCenterName { get; set; }
    }

    public class InventoryAverageConsumptionResponse
    {
        public List<InventoryAverageConsumption> Data { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetails { get; set; }
    }

    public class WarehouseResponse
    {
        public List<Warehouse> Data { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetails { get; set; }
    }
}