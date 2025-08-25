using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
namespace MISReports_Api.DAL
{
    public class IncomeExpenditureRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<IncomeExpenditureModel> GetIncomeExpenditure(string costctr, string repyear, string repmonth)
        {
            var incomeExpenditureList = new List<IncomeExpenditureModel>();

            try
            {
                Debug.WriteLine($"Parameters: costctr={costctr}, repyear={repyear}, repmonth={repmonth}");

                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    Debug.WriteLine("Database connection opened successfully");

                    string sql = @"
                        SELECT   distinct C.title_cd  ,      C.ac_cd  ,
                         NVL(ROUND(SUM(L.cl_bal),2),0.00) AS clbal,
                         sum(NVL( ROUND(B.bgt_amt,2), 0.00)) as bgt_amt ,
                         NVL(ROUND(SUM(B.bgt_amt) -SUM(L.cl_bal)),0.00)  AS varience,
                         K.ac_Nm AS catName,
                        TL.title_nm AS catCode,
                        SUBSTR(C.title_cd,1,1) AS catFlag,
                     (select dept_nm from gldeptm where dept_id =:costctr) AS cct_name
                     FROM glbgthm B,glacgrpm C ,gltitlm TL, glacctm K,
	                 (glledgrm LM
	                  LEFT OUTER JOIN gllegbal L ON  LM.gl_cd = L.gl_cd )
                           WHERE  LM.ac_cd   = C.ac_cd   AND  C.title_cd = TL.title_cd
                      and L.dept_id = B.dept_id
                      AND  L.gl_cd   = B.gl_cd  and B.bgt_amt is not null
                     AND L.yr_ind  = B.yr_ind  and b.rev_no=(select max(rev_no)from glbgthm
                        where  B.dept_id=dept_id  and B.gl_cd=gl_cd and B.yr_ind=yr_ind)
                      AND C.ac_cd = K.ac_cd
                       AND L.dept_id =  :costctr
                       AND L.yr_ind =:repyear
                       AND L.mth_ind =  :repmonth
                        AND( C.title_cd like ( 'XP%') or C.title_cd like ( 'IN%' ))
	                   Group by 8,C.title_cd ,C.ac_cd,K.ac_Nm, TL.title_nm
                       Union ALL
                   SELECT    C.title_cd,     C.ac_cd  ,
                   NVL(ROUND(SUM(L.cl_bal),2),0.00) AS clbal,
                  0.00 as bgt_amt ,  
                    -1 * NVL(ROUND(SUM(L.cl_bal),2),0.00) AS varience,
                                   K.ac_Nm AS catName,
                    TL.title_nm AS catCode,
                                 SUBSTR(C.title_cd,1,1) AS catFlag,
                  (select dept_nm from gldeptm where dept_id = :costctr ) AS cct_name
                     FROM glacgrpm C ,gltitlm TL, glacctm K,
	            (glledgrm LM
	              LEFT OUTER JOIN gllegbal L ON  LM.gl_cd = L.gl_cd )
                   WHERE     LM.ac_cd   = C.ac_cd   AND  C.title_cd = TL.title_cd
                    AND C.ac_cd = K.ac_cd
                      AND L.dept_id =  :costctr
                       AND L.yr_ind = :repyear
                       AND L.mth_ind =  :repmonth 
                       and LM.gl_cd not in ( select gl_cd   from  glbgthm where dept_id=:costctr and yr_ind=:repyear)
                             AND( C.title_cd like ( 'XP%') or C.title_cd like ( 'IN%' ))
	                  Group by 8,C.title_cd ,C.ac_cd,K.ac_Nm, TL.title_nm
                      order BY 8,1,2,4";

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
                                incomeExpenditureList.Add(new IncomeExpenditureModel
                                {
                                    CatFlag = reader["catFlag"]?.ToString(),
                                    CatCode = reader["catCode"]?.ToString(),
                                    CatName = reader["catName"]?.ToString(),
                                    AcCd = reader["ac_cd"]?.ToString(),
                                    TitleCode = reader["title_cd"]?.ToString(),
                                    Clbal = reader["clbal"] != DBNull.Value ? Convert.ToDecimal(reader["clbal"]) : 0,
                                    TotalBudget = reader["bgt_amt"] != DBNull.Value ? Convert.ToDecimal(reader["bgt_amt"]) : 0,
                                    Varience = reader["varience"] != DBNull.Value ? Convert.ToDecimal(reader["varience"]) : 0,
                                    CctName = reader["cct_name"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetIncomeExpenditure: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                throw;
            }

            return incomeExpenditureList;
        }

        // get depatment in  according to user
        public List<UserDepartment> GetDepartmentsByUser(string epfno)
        {
            var deptList = new List<UserDepartment>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT g.dept_id, g.dept_nm   
                  FROM rep_roles_cct cct, gldeptm g, rep_role_new r
                  WHERE r.roleid=cct.roleid AND 
                 cct.lvl_no=0 and cct.costcentre =g.dept_id
                  and r.epf_no=:epfno order by g.dept_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("epfno", epfno));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            deptList.Add(new UserDepartment
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
        // get company byUser
        public List<UserCompanyInfo> GetCompaniesByUserlevel(string epfno, string lvl_no)
        {
            var ucompanies = new List<UserCompanyInfo>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                      SELECT g.comp_id, g.comp_nm   FROM 
                   rep_roles_cct cct, glcompm g, rep_role_new r
                  WHERE g.status=2 and r.roleid=cct.roleid
                   and cct.costcentre =g.comp_id
                    and r.epf_no= :epfno 
                     and cct.lvl_no= :lvl_no 
                     order by g.comp_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("epfno", epfno));
                    cmd.Parameters.Add(new OracleParameter("lvl_no", lvl_no));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ucompanies.Add(new UserCompanyInfo
                            {
                                CompId = reader["comp_id"]?.ToString(),
                                CompName = reader["comp_nm"]?.ToString()
                            });
                        }
                    }
                }
            }

            return ucompanies;
        }


    }
}

