using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
    //get one  company details 
{     public class TrialBalanceRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultOracle"].ConnectionString;

        public List<TrialBalanceModel> GetTrialBalance(string costctr, string repyear, string repmonth)
        {
            var trialBalanceList = new List<TrialBalanceModel>();

            try
            {
                Debug.WriteLine($"Parameters: costctr={costctr}, repyear={repyear}, repmonth={repmonth}");

                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    Debug.WriteLine("Database connection opened successfully");

                    string sql = @"
                        SELECT
                            TRIM(glledgrm.ac_cd) AS ac_cd,
                            glledgrm.gl_nm,
                            CASE
                                WHEN SUBSTR(glledgrm.ac_cd, 1, 1) = 'A' THEN 'A'
                                WHEN SUBSTR(glledgrm.ac_cd, 1, 1) = 'E' THEN 'E'
                                WHEN SUBSTR(glledgrm.ac_cd, 1, 1) = 'L' THEN 'L'
                                ELSE 'R'
                            END AS titile_flag,
                            ROUND(SUM(gllegbal.op_bal), 2) AS op_sbal,
                            ROUND(SUM(gllegbal.dr_amt), 2) AS dr_samt,
                            ROUND(SUM(gllegbal.cr_amt), 2) AS cr_samt,
                            ROUND(SUM(gllegbal.cl_bal), 2) AS cl_sbal,
                            (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                        FROM gllegbal, glledgrm, glacgrpm, gltitlm
                        WHERE glledgrm.gl_cd = gllegbal.gl_cd
                          AND glledgrm.status = 2
                          AND glledgrm.ac_cd = glacgrpm.ac_cd
                          AND glacgrpm.title_cd = gltitlm.title_cd
                          AND gllegbal.dept_id = :costctr
                          AND gllegbal.yr_ind = :repyear
                          AND gllegbal.mth_ind = :repmonth
                          AND gltitlm.title_cd LIKE 'TB%'
                        GROUP BY glledgrm.ac_cd, glledgrm.gl_nm
                        ORDER BY glledgrm.ac_cd";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("costctr", costctr);
                        cmd.Parameters.Add("repyear", repyear);
                        cmd.Parameters.Add("repmonth", repmonth);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                trialBalanceList.Add(new TrialBalanceModel
                                {
                                    AcCd = reader["ac_cd"]?.ToString(),
                                    GlName = reader["gl_nm"]?.ToString(),
                                    TitleFlag = reader["titile_flag"]?.ToString(),
                                    OpSbal = reader["op_sbal"] != DBNull.Value ? Convert.ToDecimal(reader["op_sbal"]) : 0,
                                    DrSamt = reader["dr_samt"] != DBNull.Value ? Convert.ToDecimal(reader["dr_samt"]) : 0,
                                    CrSamt = reader["cr_samt"] != DBNull.Value ? Convert.ToDecimal(reader["cr_samt"]) : 0,
                                    ClSbal = reader["cl_sbal"] != DBNull.Value ? Convert.ToDecimal(reader["cl_sbal"]) : 0,
                                    CctName = reader["cct_name"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetTrialBalance: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                throw;
            }

            return trialBalanceList;
        }

        // get depatment in  each selected reagion  wise 
        public List<RegionDepartment> GetDepartmentsByRegion(string region)
        {
            var deptList = new List<RegionDepartment>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT dept_id, dept_nm
                    FROM gldeptm
                    WHERE status = 2
                      AND comp_id IN (
                        SELECT comp_id
                        FROM glcompm
                        WHERE status = 2
                          AND (
                            parent_id LIKE :region||'%'
                            OR Grp_comp LIKE :region||'%'
                            OR comp_id LIKE :region||'%'
                          )
                    )
                    ORDER BY dept_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("region", region));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            deptList.Add(new RegionDepartment
                            {
                                DeptId = reader["dept_id"]?.ToString(),
                                DeptName = reader["dept_nm"]?.ToString()
                            });
                        }
                    }
                }
            }
           return deptList;
        }
        // get company bylevel
        public List<CompanyInfo> GetCompaniesByLevel(string lvl_no)
        {
            var companies = new List<CompanyInfo>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
            SELECT comp_id, comp_nm 
            FROM glcompm 
            WHERE status = 2 
              AND lvl_no = :lvl_no 
            ORDER BY comp_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("lvl_no", lvl_no));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            companies.Add(new CompanyInfo
                            {
                                CompId = reader["comp_id"]?.ToString(),
                                CompName = reader["comp_nm"]?.ToString()
                            });
                        }
                    }
                }
            }

            return companies;
        }


    }
}
