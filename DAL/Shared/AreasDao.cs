using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class AreasDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage);
        }

        public List<AreaBulkModel> GetAreas()
        {
            var areasList = new List<AreaBulkModel>();

            using (var conn = _dbConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    string sql = "SELECT area_code, area_name FROM areas ORDER BY area_name";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var area = new AreaBulkModel
                            {
                                AreaCode = reader[0]?.ToString().Trim(),
                                AreaName = reader[1]?.ToString().Trim()
                            };

                            areasList.Add(area);
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    throw new Exception("Error retrieving areas data: " + ex.Message, ex);
                }
            }

            return areasList;
        }
    }
}
