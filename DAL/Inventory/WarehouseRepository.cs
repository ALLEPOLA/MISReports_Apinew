using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class WarehouseRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<WarehouseModel> GetWarehousesByEpf(string epfNo)
        {
            var warehouses = new List<WarehouseModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT DISTINCT(wrh_cd) AS WarehouseCode
                        FROM inwrhm 
                        WHERE status = 2
                          AND TRIM(dept_id) IN (
                                SELECT TRIM(costcentre) 
                                FROM rep_roles_cct_new cct, rep_role_new r
                                WHERE r.roleid = cct.roleid
                                  AND cct.lvl_no = 0
                                  AND r.epf_no = :epfno
                          )";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("epfno", epfNo);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                warehouses.Add(new WarehouseModel
                                {
                                    WarehouseCode = reader["WarehouseCode"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetWarehousesByEpf: {ex.Message}");
                throw;
            }

            return warehouses;
        }
    }
}
