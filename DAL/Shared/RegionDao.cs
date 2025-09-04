using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class RegionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage);
        }

        public List<RegionBulkModel> GetRegion()
        {
            var regionList = new List<RegionBulkModel>();

            using (var conn = _dbConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    string sql = "SELECT distinct region FROM areas WHERE region NOT IN ('L','T1')";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var region = new RegionBulkModel
                            {
                                RegionCode = reader[0]?.ToString().Trim()
                            };

                            regionList.Add(region);
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    throw new Exception("Error retrieving region data: " + ex.Message, ex);
                }
            }

            return regionList;
        }
    }
}
