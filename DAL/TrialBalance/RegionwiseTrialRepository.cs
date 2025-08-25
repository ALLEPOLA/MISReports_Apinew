using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class RegionwiseTrialRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<RegionwiseTrialModel> GetRegionwiseTrialData(string companyId, string month, string year)
        {
            var results = new List<RegionwiseTrialModel>();

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

                    // First get parent company names to avoid subquery in SELECT
                    var parentCompanies = GetParentCompanyNames(connection, companyId);

                    string sql = @"
                    WITH valid_depts AS (
                        SELECT dept_id FROM gldeptm 
                        WHERE status = 2 AND comp_id IN (
                            SELECT comp_id FROM glcompm
                            WHERE comp_id = :comp_id OR parent_id = :comp_id OR grp_comp = :comp_id
                        )
                    )
                    SELECT 
                        glledgrm.ac_cd as AccountCode,
                        glledgrm.gl_nm as AccountName,
                        CASE 
                            WHEN SUBSTR(glledgrm.ac_cd,1,1) IN ('A') THEN 'A'  
                            WHEN SUBSTR(glledgrm.ac_cd,1,1) IN ('E') THEN 'E'
                            WHEN SUBSTR(glledgrm.ac_cd,1,1) IN ('L') THEN 'L' 
                            ELSE 'R' 
                        END as TitleFlag,
                        CASE 
                            WHEN gldeptm.comp_id = :comp_id THEN gldeptm.dept_id  
                            WHEN glcompm.parent_id = :comp_id THEN glcompm.comp_id
                            WHEN glcompm.grp_comp = :comp_id THEN glcompm.parent_id  
                            ELSE '' 
                        END as CostCenter,
                        CASE 
                            WHEN gldeptm.comp_id = :comp_id THEN gldeptm.dept_nm  
                            WHEN glcompm.parent_id = :comp_id THEN glcompm.comp_nm
                            WHEN glcompm.grp_comp = :comp_id THEN :parent_company_name  
                            ELSE '' 
                        END as CompanyName,
                        gldeptm.dept_id as DepartmentId,
                        ROUND(SUM(gllegbal.op_bal),2) AS OpeningBalance,
                        ROUND(SUM(gllegbal.dr_amt),2) AS DebitAmount,
                        ROUND(SUM(gllegbal.cr_amt),2) AS CreditAmount,
                        ROUND(SUM(gllegbal.cl_bal),2) AS ClosingBalance
                    FROM 
                        gllegbal, 
                        glledgrm, 
                        glacgrpm, 
                        gltitlm, 
                        gldeptm,
                        glcompm
                    WHERE 
                        glledgrm.gl_cd = gllegbal.gl_cd
                        AND glledgrm.ac_cd = glacgrpm.ac_cd
                        AND gllegbal.dept_id = gldeptm.dept_id 
                        AND glcompm.comp_id = gldeptm.comp_id 
                        AND glacgrpm.dept_id = '900.00'
                        AND gldeptm.status = 2 
                        AND glcompm.status = 2
                        AND glacgrpm.title_cd = gltitlm.title_cd
                        AND glledgrm.status = 2
                        AND gllegbal.dept_id IN (SELECT dept_id FROM valid_depts)
                        AND gllegbal.yr_ind = :year
                        AND gllegbal.mth_ind = :month
                        AND gltitlm.title_cd LIKE 'TB%'
                    GROUP BY 
                        glledgrm.ac_cd, 
                        glledgrm.gl_nm,
                        CASE 
                            WHEN gldeptm.comp_id = :comp_id THEN gldeptm.dept_id  
                            WHEN glcompm.parent_id = :comp_id THEN glcompm.comp_id
                            WHEN glcompm.grp_comp = :comp_id THEN glcompm.parent_id  
                            ELSE '' 
                        END,
                        CASE 
                            WHEN gldeptm.comp_id = :comp_id THEN gldeptm.dept_nm  
                            WHEN glcompm.parent_id = :comp_id THEN glcompm.comp_nm
                            WHEN glcompm.grp_comp = :comp_id THEN :parent_company_name  
                            ELSE '' 
                        END,
                        gldeptm.dept_id
                    ORDER BY CostCenter";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.BindByName = true;

                        // Add parameters with correct OracleDbTypes
                        command.Parameters.Add("comp_id", OracleDbType.Char).Value = companyId;
                        command.Parameters.Add("month", OracleDbType.Int32).Value = monthInt;
                        command.Parameters.Add("year", OracleDbType.Int32).Value = yearInt;
                        command.Parameters.Add("parent_company_name", OracleDbType.Varchar2).Value =
                            parentCompanies.ContainsKey(companyId) ? parentCompanies[companyId] : "";

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new RegionwiseTrialModel
                                {
                                    AccountCode = reader["AccountCode"].ToString(),
                                    AccountName = reader["AccountName"].ToString(),
                                    TitleFlag = reader["TitleFlag"].ToString(),
                                    CostCenter = reader["CostCenter"].ToString(),
                                    CompanyName = reader["CompanyName"].ToString(),
                                    DepartmentId = reader["DepartmentId"].ToString(),
                                    OpeningBalance = SafeGetDecimal(reader["OpeningBalance"]),
                                    DebitAmount = SafeGetDecimal(reader["DebitAmount"]),
                                    CreditAmount = SafeGetDecimal(reader["CreditAmount"]),
                                    ClosingBalance = SafeGetDecimal(reader["ClosingBalance"])
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

        private Dictionary<string, string> GetParentCompanyNames(OracleConnection connection, string companyId)
        {
            var result = new Dictionary<string, string>();

            string sql = @"
                SELECT DISTINCT a.comp_id, a.comp_nm 
                FROM glcompm a 
                WHERE a.comp_id IN (
                    SELECT parent_id FROM glcompm 
                    WHERE grp_comp = :comp_id AND status = 2
                )";

            using (var command = new OracleCommand(sql, connection))
            {
                command.Parameters.Add("comp_id", OracleDbType.Char).Value = companyId;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result[reader["comp_id"].ToString()] = reader["comp_nm"].ToString();
                    }
                }
            }

            return result;
        }

        private decimal SafeGetDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0m;
            return Convert.ToDecimal(value);
        }
    }
}