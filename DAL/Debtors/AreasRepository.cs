
using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL
{
    public class AreasRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixConnection"].ConnectionString;

        public List<AreaModel> GetAreas()
        {
            var areasList = new List<AreaModel>();

            using (var conn = new OleDbConnection(connectionString))
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
                            var area = new AreaModel
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
                    Console.WriteLine($"Error retrieving areas data: {ex.Message}", ex);
                    throw;
                }
            }

            return areasList;
        }
    }
}