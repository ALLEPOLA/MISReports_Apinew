using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;

namespace MISReports_Api.DAL
{
    public class ProvinceIncomeExpenditureRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvinceIncomeExpenditureModel> GetProvinceIncomeExpenditure(string compId, string repYear, string repMonth)
        {
            var provinceList = new List<ProvinceIncomeExpenditureModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = BuildFixedQuery();

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.CommandTimeout = 300;

                        cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId;
                        cmd.Parameters.Add("repyear", OracleDbType.Varchar2).Value = repYear;
                        cmd.Parameters.Add("repmonth", OracleDbType.Varchar2).Value = repMonth;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var model = new ProvinceIncomeExpenditureModel
                                {
                                    TitleCd = SafeGetString(reader, "title_cd"),
                                    Account = SafeGetString(reader, "ACCOUNT"),
                                    Actual = SafeGetDecimal(reader, "ACTUAL"),
                                    CatName = SafeGetString(reader, "CATNAME"),
                                    MaxRev = SafeGetString(reader, "MAXREV"),
                                    CatCode = SafeGetString(reader, "CATCODE"),
                                    CatFlag = SafeGetString(reader, "CATFLAG"),
                                    AreaNum = SafeGetString(reader, "Area_num"),
                                    CctName = SafeGetString(reader, "cct_nm")
                                };

                                provinceList.Add(model);
                            }
                        }
                    }

                    // Remove duplicates after query execution
                    provinceList = RemoveDuplicates(provinceList);
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return provinceList;
        }

        private string BuildFixedQuery()
        {
            // Fixed query that properly aggregates and avoids duplicates
            string fixedQuery = @"
WITH base_data AS (
    SELECT 
        C.title_cd,
        C.ac_cd,
        K.ac_Nm,
        TL.title_nm,
        L.dept_id,
        L.cl_bal,
        D.comp_id as dept_comp_id
    FROM glacgrpm C
        INNER JOIN gltitlm TL ON C.title_cd = TL.title_cd
        INNER JOIN glacctm K ON C.ac_cd = K.ac_cd
        INNER JOIN glledgrm LM ON LM.ac_cd = C.ac_cd
        LEFT OUTER JOIN gllegbal L ON LM.gl_cd = L.gl_cd
        LEFT OUTER JOIN gldeptm D ON L.dept_id = D.dept_id
    WHERE L.yr_ind = :repyear
        AND L.mth_ind = :repmonth
        AND (C.title_cd LIKE 'XP%' OR C.title_cd LIKE 'IN%')
        AND L.cl_bal IS NOT NULL
),
aggregated_data AS (
    SELECT 
        title_cd,
        ac_cd,
        ac_Nm,
        title_nm,
        -- Determine the area number consistently
        CASE 
            WHEN dept_comp_id = :compId THEN dept_id
            ELSE SUBSTR(NVL(dept_id, 'N/A'), 1, 3)
        END AS area_num,
        dept_id,
        SUM(cl_bal) as total_balance
    FROM base_data
    GROUP BY 
        title_cd,
        ac_cd,
        ac_Nm,
        title_nm,
        CASE 
            WHEN dept_comp_id = :compId THEN dept_id
            ELSE SUBSTR(NVL(dept_id, 'N/A'), 1, 3)
        END,
        dept_id
)
SELECT 
    AD.title_cd,
    AD.ac_cd AS ACCOUNT,
    NVL(ROUND(AD.total_balance, 2), 0.00) AS ACTUAL,
    AD.ac_Nm AS CATNAME,
    '' AS MAXREV,
    AD.title_nm AS CATCODE,
    SUBSTR(AD.title_cd, 1, 1) AS CATFLAG,
    AD.area_num AS Area_num,
    COALESCE(
        (SELECT dept_nm FROM gldeptm WHERE dept_id = AD.dept_id),
        (SELECT dept_nm FROM gldeptm WHERE dept_id = SUBSTR(AD.dept_id, 1, 3) || '.00'),
        'Unknown Department'
    ) AS cct_nm
FROM aggregated_data AD
ORDER BY 
    SUBSTR(AD.title_cd, 1, 1),  -- CATFLAG
    AD.title_cd,                -- title_cd  
    AD.ac_cd,                   -- ACCOUNT
    AD.ac_Nm                    -- CATNAME";

            return fixedQuery;
        }

        // Method to remove duplicates based on key fields
        private List<ProvinceIncomeExpenditureModel> RemoveDuplicates(List<ProvinceIncomeExpenditureModel> originalList)
        {
            var uniqueRecords = new Dictionary<string, ProvinceIncomeExpenditureModel>();

            foreach (var record in originalList)
            {
                // Create a unique key based on the main identifying fields
                string uniqueKey = $"{record.TitleCd}_{record.Account}_{record.AreaNum}";

                if (uniqueRecords.ContainsKey(uniqueKey))
                {
                    // If duplicate found, sum the actual values
                    uniqueRecords[uniqueKey].Actual += record.Actual;
                 
                }
                else
                {
                    uniqueRecords.Add(uniqueKey, new ProvinceIncomeExpenditureModel
                    {
                        TitleCd = record.TitleCd,
                        Account = record.Account,
                        Actual = record.Actual,
                        CatName = record.CatName,
                        MaxRev = record.MaxRev,
                        CatCode = record.CatCode,
                        CatFlag = record.CatFlag,
                        AreaNum = record.AreaNum,
                        CctName = record.CctName
                    });
                }
            }

            return uniqueRecords.Values.OrderBy(x => x.CatFlag)
                                     .ThenBy(x => x.TitleCd)
                                     .ThenBy(x => x.Account)
                                     .ThenBy(x => x.CatName)
                                     .ToList();
        }



        private string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private decimal SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
        }
    }
}