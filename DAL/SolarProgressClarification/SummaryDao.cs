using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.SolarProgressClarification
{
    public class SummaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                // Connection string verification
                System.Diagnostics.Debug.WriteLine($"=== Testing Connection ===");
                System.Diagnostics.Debug.WriteLine($"Connection String: {_dbConnection.ConnectionString}");

                // Test if we can parse the connection string first
                var builder = new OleDbConnectionStringBuilder(_dbConnection.ConnectionString);
                System.Diagnostics.Debug.WriteLine($"Data Source: {builder.DataSource}");
                System.Diagnostics.Debug.WriteLine($"Provider: {builder.Provider}");

                // Check if required properties exist
                if (string.IsNullOrEmpty(builder.DataSource))
                {
                    errorMessage = "Data Source is missing from connection string";
                    return false;
                }

                if (string.IsNullOrEmpty(builder.Provider))
                {
                    errorMessage = "Provider is missing from connection string";
                    return false;
                }

                using (var conn = _dbConnection.GetConnection())
                {
                    System.Diagnostics.Debug.WriteLine("Attempting to open connection...");
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine("Connection opened successfully.");
                    return true;
                }
            }
            catch (OleDbException ex)
            {
                System.Diagnostics.Debug.WriteLine($"OLE DB Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error Code: {ex.ErrorCode}");

                if (ex.Errors != null && ex.Errors.Count > 0)
                {
                    for (int i = 0; i < ex.Errors.Count; i++)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error {i}: SQLState={ex.Errors[i].SQLState}, NativeError={ex.Errors[i].NativeError}");
                    }
                }

                errorMessage = $"Database connection failed: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                errorMessage = $"Connection test failed: {ex.Message}";
                return false;
            }
        }

        public List<SolarProgressSummaryModel> GetSummaryReport(SolarProgressRequest request)
        {
            var results = new List<SolarProgressSummaryModel>();

            try
            {
                System.Diagnostics.Debug.WriteLine("=== START GetSummaryReport ===");

                string sql = BuildSqlQuery(request);
                System.Diagnostics.Debug.WriteLine($"Generated SQL: {sql}");
                System.Diagnostics.Debug.WriteLine($"Parameters: BillCycle={request.BillCycle}, AreaCode={request.AreaCode}, ProvCode={request.ProvCode}, Region={request.Region}");

                using (var conn = _dbConnection.GetConnection())
                {
                    System.Diagnostics.Debug.WriteLine("Attempting to open connection...");
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine("Connection opened successfully.");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // Add parameters in order
                        System.Diagnostics.Debug.WriteLine("Adding parameter 1: BillCycle");
                        cmd.Parameters.AddWithValue("", request.BillCycle);

                        if (request.ReportType == SolarReportType.Area && !string.IsNullOrEmpty(request.AreaCode))
                        {
                            System.Diagnostics.Debug.WriteLine("Adding parameter 2: AreaCode");
                            cmd.Parameters.AddWithValue("", request.AreaCode);
                        }
                        else if (request.ReportType == SolarReportType.Province && !string.IsNullOrEmpty(request.ProvCode))
                        {
                            System.Diagnostics.Debug.WriteLine("Adding parameter 2: ProvCode");
                            cmd.Parameters.AddWithValue("", request.ProvCode);
                        }
                        else if (request.ReportType == SolarReportType.Region && !string.IsNullOrEmpty(request.Region))
                        {
                            System.Diagnostics.Debug.WriteLine("Adding parameter 2: Region");
                            cmd.Parameters.AddWithValue("", request.Region);
                        }

                        System.Diagnostics.Debug.WriteLine("Executing query...");
                        using (var reader = cmd.ExecuteReader())
                        {
                            System.Diagnostics.Debug.WriteLine("Reading results...");
                            while (reader.Read())
                            {
                                var solarProgress = MapSolarProgressFromReader(reader);
                                results.Add(solarProgress);
                            }
                            System.Diagnostics.Debug.WriteLine($"Read {results.Count} records.");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== END GetDetailedReport (Success) ===");
                return results;
            }
            catch (OleDbException ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== OLE DB ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error Code: {ex.ErrorCode}");

                if (ex.Errors != null && ex.Errors.Count > 0)
                {
                    for (int i = 0; i < ex.Errors.Count; i++)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error {i}: SQLState={ex.Errors[i].SQLState}, NativeError={ex.Errors[i].NativeError}");
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GENERAL ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().FullName}");
                throw;
            }
        }

        private string BuildSqlQuery(SolarProgressRequest request)
        {
            string baseQuery = @"
        SELECT 
            a.region,
            a.prov_code,
            p.prov_name,
            a.area_name,
            n.chg_code,
            COUNT(*) AS record_count,
            SUM(n.cap_chg) AS cap_chg
        FROM netmtchg n
        INNER JOIN areas a ON a.area_code = n.area_code
        INNER JOIN provinces p ON a.prov_code = p.prov_code
        WHERE n.bill_cycle = ?";

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery += " AND n.area_code = ? ";
                    break;

                case SolarReportType.Province:
                    baseQuery += " AND a.prov_code = ? ";
                    break;

                case SolarReportType.Region:
                    baseQuery += " AND a.region = ? ";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    // no extra filter
                    break;
            }

            baseQuery += " GROUP BY a.region, a.prov_code, p.prov_name, a.area_name, n.chg_code";
            baseQuery += " ORDER BY a.region ASC, p.prov_name ASC, a.area_name ASC, n.chg_code";

            return baseQuery;
        }


        private SolarProgressSummaryModel MapSolarProgressFromReader(OleDbDataReader reader)
        {
            // DEBUG: Log all available columns
            var availableColumns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                availableColumns.Add(reader.GetName(i));
            }
            System.Diagnostics.Debug.WriteLine($"Available columns: {string.Join(", ", availableColumns)}");

            // DEBUG: Check the actual net_chg value
            string rawNetChg = GetColumnValue(reader, "net_chg");
            System.Diagnostics.Debug.WriteLine($"Raw net_chg value: '{rawNetChg}'");

            var model = new SolarProgressSummaryModel
            {
                Region = GetColumnValue(reader, "region"),
                Province = GetColumnValue(reader, "prov_name"),
                Area = GetColumnValue(reader, "area_name"),
                Description = MapDescription(GetColumnValue(reader, "chg_code")),
                Count = GetIntValue(reader, "record_count"),
                Capacity = GetDecimalValue(reader, "cap_chg"),
                ErrorMessage = string.Empty
            };

            return model;
        }

        // Helper methods to safely get column values
        private string GetColumnValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? null : value.ToString().Trim();
            }
            catch (IndexOutOfRangeException)
            {
                System.Diagnostics.Debug.WriteLine($"Column '{columnName}' not found in result set");
                return null;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
            }
            catch (IndexOutOfRangeException)
            {
                System.Diagnostics.Debug.WriteLine($"Column '{columnName}' not found in result set");
                return 0;
            }
        }



        public static string MapDescription(string changeCode)
        {
            if (string.IsNullOrEmpty(changeCode))
                return "Unknown";

            string trimmedCode = changeCode.Trim();

            switch (trimmedCode)
            {
                case "C":
                    return "Capacity Change";
                case "N":
                    return "New";
                case "S":
                    return "Stop";
                case "Y":
                    return "Net Type Change";
                case "F":
                case "T":
                    return "Area Change";
                default:
                    return "Unknown";
            }
        }

        private int GetIntValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
            catch (IndexOutOfRangeException)
            {
                System.Diagnostics.Debug.WriteLine($"Column '{columnName}' not found in result set");
                return 0;
            }
        }


    }

}