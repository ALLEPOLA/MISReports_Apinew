using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class InventoryAverageConsumptionRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<InventoryAverageConsumption> GetAverageConsumption(string costCenter, string warehouseCode, DateTime fromDate, DateTime toDate)
        {
            var consumptionList = new List<InventoryAverageConsumption>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
SELECT 
    T1.wrh_cd,
    T1.mat_cd,
    T2.mat_nm,
    T1.grade_cd,
    T1.unit_price,
    T1.qty_on_hand,
    NVL(SUM(CASE WHEN T3.add_deduct='F' THEN T3.trx_qty
                 WHEN T3.add_deduct='T' THEN -T3.trx_qty 
                 ELSE 0.00 END), 0.00) AS Trx,
    (NVL(SUM(CASE WHEN T3.add_deduct='F' THEN T3.trx_qty
                  WHEN T3.add_deduct='T' THEN -T3.trx_qty 
                  ELSE 0.00 END), 0.00) / 
    NULLIF(ROUND(MONTHS_BETWEEN(TO_DATE(:toDate,'yyyy/mm/dd'),
                              TO_DATE(:fromDate,'yyyy/mm/dd')), 0), 0)) AS Average,
    (SELECT dept_nm FROM gldeptm WHERE dept_id=:costCenter) AS CCT_NAME
FROM 
    inmatm T2,
    inwrhmtm T1
LEFT OUTER JOIN 
    inpostmt T3 ON T1.dept_id = T3.dept_id 
               AND T1.mat_cd = T3.mat_cd 
               AND T1.grade_cd = T3.grade_cd 
               AND T1.wrh_cd = T3.wrh_cd 
               AND (T3.trx_type LIKE 'ISS%' OR 
                   (T3.trx_type IN ('RECEIPT') AND T3.doc_pf IN ('RTV','RTV-CL')))
               AND T3.trx_dt >= TO_DATE(:fromDate,'yyyy/mm/dd')
               AND T3.trx_dt <= TO_DATE(:toDate,'yyyy/mm/dd')
WHERE 
    T2.mat_cd = T1.mat_cd
    AND T1.dept_id = :costCenter
    AND T1.status = 2 
    AND T1.wrh_cd = :warehouseCode
GROUP BY 
    T1.wrh_cd, T1.mat_cd, T2.mat_nm, T1.grade_cd, T1.qty_on_hand, T1.unit_price
ORDER BY 
    T1.wrh_cd ASC, T1.mat_cd ASC, T2.mat_nm ASC, T1.grade_cd ASC, T1.qty_on_hand ASC";
                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("costCenter", costCenter);
                        cmd.Parameters.Add("warehouseCode", warehouseCode);
                        cmd.Parameters.Add("fromDate", fromDate.ToString("yyyy/MM/dd"));
                        cmd.Parameters.Add("toDate", toDate.ToString("yyyy/MM/dd"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                consumptionList.Add(new InventoryAverageConsumption
                                {
                                    WarehouseCode = reader["wrh_cd"]?.ToString(),
                                    MaterialCode = reader["mat_cd"]?.ToString(),
                                    MaterialName = reader["mat_nm"]?.ToString(),
                                    GradeCode = reader["grade_cd"]?.ToString(),
                                    UnitPrice = reader["unit_price"] != DBNull.Value ? Convert.ToDecimal(reader["unit_price"]) : 0,
                                    QuantityOnHand = reader["qty_on_hand"] != DBNull.Value ? Convert.ToDecimal(reader["qty_on_hand"]) : 0,
                                    TransactionQuantity = reader["Trx"] != DBNull.Value ? Convert.ToDecimal(reader["Trx"]) : 0,
                                    AverageConsumption = reader["Average"] != DBNull.Value ? Convert.ToDecimal(reader["Average"]) : 0,
                                    CostCenterName = reader["CCT_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAverageConsumption: {ex.Message}");
                throw;
            }

            return consumptionList;
        }

        public List<Warehouse> GetWarehousesByEpfNo(string epfNo)
        {
            var warehouseList = new List<Warehouse>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT DISTINCT(wrh_cd) 
                        FROM inwrhm 
                        WHERE status=2
                          AND TRIM(dept_id) IN (
                              SELECT TRIM(costcentre) 
                              FROM rep_roles_cct cct, rep_role_new r 
                              WHERE r.roleid=cct.roleid 
                                AND cct.lvl_no=0  
                                AND r.epf_no=:epfNo)";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("epfNo", epfNo);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                warehouseList.Add(new Warehouse
                                {
                                    WarehouseCode = reader["wrh_cd"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetWarehousesByEpfNo: {ex.Message}");
                throw;
            }

            return warehouseList;
        }
    }
}