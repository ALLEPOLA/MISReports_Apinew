using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class ProvinceDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage);
        }

        public List<ProvinceBulkModel> GetProvince()
        {
            var provinceList = new List<ProvinceBulkModel>();

            using (var conn = _dbConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    string sql = "SELECT prov_code,prov_name FROM provinces ORDER BY prov_name";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var province = new ProvinceBulkModel
                            {
                                ProvinceCode = reader[0]?.ToString().Trim(),
                                ProvinceName = reader[1]?.ToString().Trim()
                            };

                            provinceList.Add(province);
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    throw new Exception("Error retrieving province data: " + ex.Message, ex);
                }
            }

            return provinceList;
        }
    }
}
