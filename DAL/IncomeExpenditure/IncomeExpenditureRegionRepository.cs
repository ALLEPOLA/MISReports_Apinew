using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class IncomeExpenditureRegionRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<IncomeExpenditureRegionModel> GetIncomeExpenditureRegion(string companyId, string repYear, string repMonth)
        {
            var regionList = new List<IncomeExpenditureRegionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT
                            C.title_cd,
                            C.ac_cd AS ACCOUNT,
                            NVL(ROUND(SUM(L.cl_bal),2),0.00) AS ACTUAL,
                            K.ac_Nm AS CATNAME,
                            '' AS MAXREV,
                            TL.title_nm AS CATCODE,
                            SUBSTR(C.title_cd,1,1) AS CATFLAG,
                            (SELECT comp_nm FROM glcompm WHERE comp_id = :company_id) AS comp_nm,
                            (SELECT CASE 
                                        WHEN lvl_no = 60 THEN comp_id 
                                        WHEN lvl_no = 50 THEN parent_id 
                                        WHEN lvl_no = 0 AND comp_id != :company_id THEN grp_comp 
                                        ELSE L.dept_id END 
                             FROM glcompm 
                             WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = L.dept_id)) AS costctr
                        FROM 
                            glacgrpm C, 
                            gltitlm TL, 
                            gldeptm, 
                            glacctm K,
                            (glledgrm LM LEFT OUTER JOIN gllegbal L ON LM.gl_cd = L.gl_cd)
                        WHERE  
                            LM.ac_cd = C.ac_cd AND  
                            C.title_cd = TL.title_cd AND
                            L.dept_id = gldeptm.dept_id AND
                            C.ac_cd = K.ac_cd AND
                            (L.dept_id IN (SELECT dept_id FROM gldeptm WHERE comp_id IN 
                                          (SELECT comp_id FROM glcompm 
                                           WHERE comp_id = :company_id 
                                              OR grp_comp = :company_id 
                                              OR parent_id = :company_id))) AND
                            L.yr_ind = :year AND
                            L.mth_ind = :month AND
                            (C.title_cd LIKE 'XP%' OR C.title_cd LIKE 'IN%')
                        GROUP BY 
                            7, C.title_cd, C.ac_cd, K.ac_Nm,  
                            (CASE WHEN gldeptm.comp_id = :company_id THEN gldeptm.dept_id ELSE SUBSTR(gldeptm.dept_id,1,3) END),
                            L.dept_id, TL.title_nm
                        ORDER BY 7,1,2,4,8";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("company_id", companyId);
                        cmd.Parameters.Add("year", repYear);
                        cmd.Parameters.Add("month", repMonth);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                regionList.Add(new IncomeExpenditureRegionModel
                                {
                                    TitleCd = reader["title_cd"]?.ToString(),
                                    Account = reader["ACCOUNT"]?.ToString(),
                                    Actual = reader["ACTUAL"] != DBNull.Value ? Convert.ToDecimal(reader["ACTUAL"]) : 0,
                                    CatName = reader["CATNAME"]?.ToString(),
                                    MaxRev = reader["MAXREV"]?.ToString(),
                                    CatCode = reader["CATCODE"]?.ToString(),
                                    CatFlag = reader["CATFLAG"]?.ToString(),
                                    CompName = reader["comp_nm"]?.ToString(),
                                    CostCtr = reader["costctr"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetIncomeExpenditureRegion: {ex.Message}");
                throw;
            }

            return regionList;
        }
    }
}
