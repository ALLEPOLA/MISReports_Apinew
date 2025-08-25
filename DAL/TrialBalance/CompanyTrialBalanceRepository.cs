using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class CompanyTrialBalanceRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CompanyTrialBalanceModel> GetTrialBalanceData(string companyId, string month, string year)
        {
            var results = new List<CompanyTrialBalanceModel>();

            try
            {
                // Convert and validate parameters
                int monthInt = int.Parse(month);
                int yearInt = int.Parse(year);
                companyId = companyId?.Trim().ToUpper();

                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();

                    if (connection.State != ConnectionState.Open)
                    {
                        throw new Exception("Failed to open database connection");
                    }

                    string sql = @"
                    SELECT 
                        glledgrm.ac_cd, 
                        glledgrm.gl_nm as ac_nm, 
                        CASE 
                            WHEN substr(glledgrm.ac_cd, 1, 1) IN ('A') THEN 'A' 
                            WHEN substr(glledgrm.ac_cd, 1, 1) IN ('E') THEN 'E'
                            WHEN substr(glledgrm.ac_cd, 1, 1) IN ('L') THEN 'L' 
                            ELSE 'R' 
                        END as title_flag,
                        CASE 
                            WHEN gldeptm.comp_id = :comp_id THEN 'CC -' || gldeptm.dept_id 
                            ELSE gldeptm.comp_id || ' / ' || substr(gldeptm.dept_id, 1, 3) 
                        END as costctr, 
                        (SELECT comp_nm FROM glcompm WHERE comp_id = :comp_id) as comp_nm, 
                        ROUND(SUM(gllegbal.op_bal), 2) AS op_bal,
                        ROUND(SUM(gllegbal.dr_amt), 2) AS dr_amt, 
                        ROUND(SUM(gllegbal.cr_amt), 2) AS cr_amt, 
                        ROUND(SUM(gllegbal.cl_bal), 2) AS cl_bal  
                    FROM 
                        gllegbal, 
                        glledgrm, 
                        glacgrpm, 
                        gltitlm, 
                        gldeptm 
                    WHERE 
                        glledgrm.gl_cd = gllegbal.gl_cd 
                        AND glledgrm.ac_cd = glacgrpm.ac_cd 
                        AND gllegbal.dept_id = gldeptm.dept_id 
                        AND glacgrpm.title_cd = gltitlm.title_cd 
                        AND gllegbal.dept_id IN (
                            SELECT dept_id FROM gldeptm 
                            WHERE status = 2 
                            AND comp_id IN (
                                SELECT comp_id FROM glcompm 
                                WHERE (comp_id = :comp_id OR parent_id = :comp_id)
                                AND status = 2
                            )
                        ) 
                        AND gllegbal.mth_ind = :month  
                        AND gllegbal.yr_ind = :year  
                        AND gltitlm.title_cd LIKE 'TB%' 
                    GROUP BY 
                        CASE 
                            WHEN gldeptm.comp_id = :comp_id THEN 'CC -' || gldeptm.dept_id 
                            ELSE gldeptm.comp_id || ' / ' || substr(gldeptm.dept_id, 1, 3) 
                        END, 
                        glledgrm.ac_cd,  
                        glledgrm.gl_nm 
                    ORDER BY costctr";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.BindByName = true;
                        
                        // Add parameters with correct OracleDbTypes
                        command.Parameters.Add("comp_id", OracleDbType.Char).Value = companyId;
                        command.Parameters.Add("month", OracleDbType.Int32).Value = monthInt;
                        command.Parameters.Add("year", OracleDbType.Int32).Value = yearInt;

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new CompanyTrialBalanceModel
                                {
                                    AccountCode = reader["ac_cd"].ToString(),
                                    AccountName = reader["ac_nm"].ToString(),
                                    TitleFlag = reader["title_flag"].ToString(),
                                    CostCenter = reader["costctr"].ToString(),
                                    CompanyName = reader["comp_nm"].ToString(),
                                    OpeningBalance = SafeGetDecimal(reader["op_bal"]),
                                    DebitAmount = SafeGetDecimal(reader["dr_amt"]),
                                    CreditAmount = SafeGetDecimal(reader["cr_amt"]),
                                    ClosingBalance = SafeGetDecimal(reader["cl_bal"])
                                });
                            }
                        }
                    }
                }
                return results;
            }
            catch (OracleException ex)
            {
                throw new Exception($"Oracle Error {ex.Number}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database operation failed: {ex.Message}", ex);
            }
        }

        private decimal SafeGetDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0m;
            return Convert.ToDecimal(value);
        }
    }
}