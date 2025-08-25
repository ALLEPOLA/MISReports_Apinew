using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL
{
    public class DebtorsRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixConnection"].ConnectionString;

        public List<DebtorsModel> GetDebtorsData(string opt, string cycle, string areaCode)
        {
            var debtorsList = new List<DebtorsModel>();

            using (var conn = new OleDbConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string sql = BuildSqlQuery(opt, cycle, areaCode);

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var debtor = new DebtorsModel
                            {
                                Type = "Ordinary",
                                CustType = GetCustomerTypeDescription(reader[0]?.ToString().Trim()),
                                TotDebtors = reader[1] != DBNull.Value ? Convert.ToDecimal(reader[1]) : 0,
                                Month01 = reader[2] != DBNull.Value ? Convert.ToDecimal(reader[2]) : 0,
                                Month02 = reader[3] != DBNull.Value ? Convert.ToDecimal(reader[3]) : 0,
                                Month03 = reader[4] != DBNull.Value ? Convert.ToDecimal(reader[4]) : 0,
                                Month04 = reader[5] != DBNull.Value ? Convert.ToDecimal(reader[5]) : 0
                            };

                            debtorsList.Add(debtor);
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    Console.WriteLine($"Error retrieving debtors data: {ex.Message}", ex);
                }
            }

            return debtorsList;
        }

        private string BuildSqlQuery(string opt, string cycle, string areaCode)
        {
            string baseSelect = @"SELECT cust_type, 
                sum(m00+m01+m02+m03+m04+m05+m06+m07+m08+m09+m10+m11+m12+m13+m14+m15+m16+m17+m18+m19+m20+m21+m22+m23+m24+m25+m26+m27+m28+m29+m30+m31+m32+m33+m34+m35+m36+m37+m38+m39+m40+m41+m42+m43+m44+m45+m46+m47+m48+m49+m50+m51+m52+m53+m54+m55+m56+m57+m58+m59+m60+m61),
                sum(m00),
                sum(m01),
                sum(m02),
                sum(m03 + m04 + m05 + m06 + m07 + m08 + m09 + m10 + m11 + m12 + m13 + m14 + m15 + m16 + m17 + m18 + m19 + m20 + m21 + m22 + m23 + m24 + m25 + m26 + m27 + m28 + m29 + m30 + m31 + m32 + m33 + m34 + m35 + m36 + m37 + m38 + m39 + m40 + m41 + m42 + m43 + m44 + m45 + m46 + m47 + m48 + m49 + m50 + m51 + m52 + m53 + m54 + m55 + m56 + m57 + m58 + m59 + m60 + m61) ";

            switch (opt?.ToUpper())
            {
                case "A":
                    return $@"{baseSelect}
                        FROM agesmry 
                        WHERE bill_cycle = '{cycle}' AND area_code = '{areaCode}' 
                        GROUP BY cust_type";

                case "P":
                    return $@"{baseSelect}
                        FROM agesmry a1, areas a2 
                        WHERE a1.bill_cycle = '{cycle}' 
                        AND a1.area_code = a2.area_code 
                        AND a2.prov_code = '{areaCode}' 
                        GROUP BY a1.cust_type";

                case "D":
                    return $@"{baseSelect}
                        FROM agesmry a1, areas a2 
                        WHERE a1.bill_cycle = '{cycle}' 
                        AND a1.area_code = a2.area_code 
                        AND a2.region = '{areaCode}' 
                        GROUP BY a1.cust_type";

                case "E":
                    return $@"{baseSelect}
                        FROM agesmry 
                        WHERE bill_cycle = '{cycle}' 
                        GROUP BY cust_type";

                default:
                    throw new ArgumentException($"Invalid option: {opt}");
            }
        }

        private string GetCustomerTypeDescription(string custType)
        {
            if (string.IsNullOrEmpty(custType))
                return "Unknown";

            switch (custType.ToUpper())
            {
                case "F":
                    return "Finalized";
                case "G":
                    return "Govt.";
                case "A":
                    return "Non Govt.";
                default:
                    return custType;
            }
        }
    }
}