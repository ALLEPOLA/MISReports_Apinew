using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class MaterialRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultOracle"].ConnectionString;

        // Get name and code of active materials
        public List<Material> GetActiveMaterials()
        {
            var materials = new List<Material>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = "SELECT mat_cd, mat_nm FROM inmatm ORDER BY mat_cd";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        materials.Add(new Material
                        {
                            MatCd = reader["mat_cd"]?.ToString().Trim(),
                            MatNm = reader["mat_nm"]?.ToString().Trim()
                        });
                    }
                }
            }

            return materials;
        }

        // Get region-wise material details
        public List<MaterialReagionStock> GetMaterialStocks()
        {
            var stocks = new List<MaterialReagionStock>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT 
                        (CASE WHEN c.lvl_no = 60 THEN c.parent_id ELSE c.Grp_comp END) AS Region,
                        i.mat_cd,
                        SUM(i.qty_on_hand) AS qty_on_hand
                    FROM inwrhmtm i
                    JOIN gldeptm d ON i.dept_id = d.dept_id
                    JOIN glcompm c ON d.comp_id = c.comp_id
                    WHERE i.mat_cd LIKE 'D0210%'
                      AND i.status = 2
                      AND i.GRADE_CD = 'NEW'
                      AND (
                        c.parent_id LIKE 'DISCO%'
                        OR c.Grp_comp LIKE 'DISCO%'
                        OR c.comp_id LIKE 'DISCO%'
                      )
                    GROUP BY (CASE WHEN c.lvl_no = 60 THEN c.parent_id ELSE c.Grp_comp END), i.mat_cd
                    ORDER BY 1, 2";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stocks.Add(new MaterialReagionStock
                        {
                            Region = reader["Region"]?.ToString().Trim(),
                            MatCd = reader["mat_cd"]?.ToString().Trim(),
                            QtyOnHand = reader["qty_on_hand"] != DBNull.Value ? Convert.ToDecimal(reader["qty_on_hand"]) : 0
                        });
                    }
                }
            }

            return stocks;
        }

        // Get material stock balances for a material code
        public List<MaterialStockBalance> GetMaterialStockBalances(string matCd)
        {
            var balances = new List<MaterialStockBalance>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT
                        T1.MAT_CD,
                        (SELECT CASE WHEN lvl_no = 60 THEN 'DD' || SUBSTR(parent_id,6,1)
                                      ELSE 'DD' || SUBSTR(Grp_comp,6,1) END
                         FROM glcompm
                         WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = T1.dept_id)) AS region,
                        (SELECT CASE WHEN lvl_no = 60 THEN comp_id ELSE parent_id END
                         FROM glcompm
                         WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = T1.dept_id)) AS province,
                        (T1.dept_id || ' - ' || (SELECT dept_nm FROM gldeptm WHERE dept_id = T1.dept_id)) AS dept_id,
                        T2.MAT_NM,
                        T2.unit_price,
                        SUM(T1.QTY_ON_HAND) AS QTY_ON_HAND,
                        T3.reord_qty AS reorder_qty,
                        T1.UOM_CD
                    FROM INMATM T2, inwhmtdm T3, INWRHMTM T1
                    WHERE T2.MAT_CD = T1.MAT_CD
                      AND T1.dept_id = T3.dept_id
                      AND T1.wrh_cd = T3.wrh_cd
                      AND T1.mat_cd = T3.mat_cd
                      AND T1.grade_cd = T3.grade_cd
                      AND T1.dept_id IN (
                          SELECT dept_id
                          FROM gldeptm
                          WHERE comp_id IN (
                              SELECT comp_id FROM glcompm
                              WHERE status = 2
                                AND (parent_id LIKE 'DISCO%' OR Grp_comp LIKE 'DISCO%' OR comp_id LIKE 'DISCO%')
                          )
                      )
                      AND UPPER(TRIM(T1.mat_cd)) = :matCd
                      AND T1.GRADE_CD = 'NEW'
                      AND T1.status = 2
                    GROUP BY T1.MAT_CD, T2.MAT_NM, T1.UOM_CD, T1.dept_id, T2.unit_price, T3.reord_qty
                    ORDER BY 1, 2, 3, 4";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("matCd", matCd.Trim().ToUpper()));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            balances.Add(new MaterialStockBalance
                            {
                                MatCd = reader["MAT_CD"]?.ToString().Trim(),
                                Region = reader["region"]?.ToString().Trim(),
                                Province = reader["province"]?.ToString().Trim(),
                                DeptId = reader["dept_id"]?.ToString().Trim(),
                                MatNm = reader["MAT_NM"]?.ToString().Trim(),
                                UnitPrice = reader["unit_price"] != DBNull.Value ? Convert.ToDecimal(reader["unit_price"]) : 0,
                                CommittedCost = reader["QTY_ON_HAND"] != DBNull.Value ? Convert.ToDecimal(reader["QTY_ON_HAND"]) : 0,
                                UomCd = reader["UOM_CD"]?.ToString().Trim()
                            });
                        }
                    }
                }
            }

            return balances;
        }

        // Get region-wise quantity on hand by material code
        public List<MaterialReagionStock> GetMaterialStocksByMatCd(string matCd)
        {
            var stocks = new List<MaterialReagionStock>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT 
                        (CASE WHEN c.lvl_no = 60 THEN c.parent_id ELSE c.Grp_comp END) AS Region,
                        i.mat_cd,
                        SUM(i.qty_on_hand) AS qty_on_hand
                    FROM inwrhmtm i
                    JOIN gldeptm d ON i.dept_id = d.dept_id
                    JOIN glcompm c ON d.comp_id = c.comp_id
                    WHERE UPPER(TRIM(i.mat_cd)) = :matCd
                      AND i.status = 2
                      AND (
                        c.parent_id LIKE 'DISCO%'
                        OR c.Grp_comp LIKE 'DISCO%'
                        OR c.comp_id LIKE 'DISCO%'
                      )
                    GROUP BY (CASE WHEN c.lvl_no = 60 THEN c.parent_id ELSE c.Grp_comp END), i.mat_cd
                    ORDER BY 1, 2";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("matCd", matCd.Trim().ToUpper()));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stocks.Add(new MaterialReagionStock
                            {
                                Region = reader["Region"]?.ToString().Trim(),
                                MatCd = reader["mat_cd"]?.ToString().Trim(),
                                QtyOnHand = reader["qty_on_hand"] != DBNull.Value ? Convert.ToDecimal(reader["qty_on_hand"]) : 0
                            });
                        }
                    }
                }
            }

            return stocks;
        }

        // Get material stocks by province-wise
        public List<MaterialProvinceStock> GetMaterialStocksByMatCdProvinceWise(string matCd)
        {
            var stocks = new List<MaterialProvinceStock>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
            SELECT 
                (CASE WHEN c.lvl_no = 60 THEN c.comp_id ELSE c.parent_id END) AS Province,
                i.mat_cd,
                SUM(i.qty_on_hand) AS qty_on_hand
            FROM inwrhmtm i
            JOIN gldeptm d ON i.dept_id = d.dept_id
            JOIN glcompm c ON d.comp_id = c.comp_id
            WHERE UPPER(TRIM(i.mat_cd)) = :matCd
              AND i.status = 2
              AND (
                c.parent_id LIKE 'DISCO%'
                OR c.Grp_comp LIKE 'DISCO%'
                OR c.comp_id LIKE 'DISCO%'
              )
            GROUP BY (CASE WHEN c.lvl_no = 60 THEN c.comp_id ELSE c.parent_id END), i.mat_cd
            ORDER BY 1, 2";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("matCd", matCd.Trim().ToUpper()));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stocks.Add(new MaterialProvinceStock
                            {
                                Province = reader["Province"]?.ToString().Trim(),
                                MatCd = reader["mat_cd"]?.ToString().Trim(),
                                QtyOnHand = reader["qty_on_hand"] != DBNull.Value ? Convert.ToDecimal(reader["qty_on_hand"]) : 0
                            });
                        }
                    }
                }
            }

            return stocks;
        }

       
    }
}
