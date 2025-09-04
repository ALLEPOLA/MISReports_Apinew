using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using NLog;

namespace MISReports_Api.DAL.SolarProgressClarification
{
    public class DetailedDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger(); // Add logger instance

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                logger.Debug("=== Testing Connection ===");
                logger.Debug($"Connection String: {_dbConnection.ConnectionString}");

                var builder = new OleDbConnectionStringBuilder(_dbConnection.ConnectionString);
                logger.Debug($"Data Source: {builder.DataSource}");
                logger.Debug($"Provider: {builder.Provider}");

                if (string.IsNullOrEmpty(builder.DataSource))
                {
                    errorMessage = "Data Source is missing from connection string";
                    logger.Warn(errorMessage);
                    return false;
                }

                if (string.IsNullOrEmpty(builder.Provider))
                {
                    errorMessage = "Provider is missing from connection string";
                    logger.Warn(errorMessage);
                    return false;
                }

                using (var conn = _dbConnection.GetConnection())
                {
                    logger.Debug("Attempting to open connection...");
                    conn.Open();
                    logger.Info("Database connection opened successfully.");
                    return true;
                }
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "OLE DB Error occurred during connection test");

                if (ex.Errors != null && ex.Errors.Count > 0)
                {
                    for (int i = 0; i < ex.Errors.Count; i++)
                    {
                        logger.Error($"Error {i}: SQLState={ex.Errors[i].SQLState}, NativeError={ex.Errors[i].NativeError}");
                    }
                }

                errorMessage = $"Database connection failed: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "General error occurred during connection test");
                errorMessage = $"Connection test failed: {ex.Message}";
                return false;
            }
        }

        public List<SolarProgressDetailedModel> GetDetailedReport(SolarProgressRequest request)
        {
            var results = new List<SolarProgressDetailedModel>();

            try
            {
                logger.Info("=== START GetDetailedReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, ReportType={request.ReportType}, AreaCode={request.AreaCode}, ProvCode={request.ProvCode}, Region={request.Region}");

                string sql = BuildSqlQuery(request);
                logger.Debug($"Generated SQL: {sql}");

                using (var conn = _dbConnection.GetConnection())
                {
                    logger.Debug("Opening database connection...");
                    conn.Open();
                    logger.Debug("Database connection opened successfully.");

                    logger.Debug("Loading area code to name mapping...");
                    var areaMapping = GetAreaNameMapping(conn);
                    logger.Info($"Loaded {areaMapping.Count} area mappings");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        logger.Debug("Adding parameter: BillCycle = {BillCycle}", request.BillCycle);
                        cmd.Parameters.AddWithValue("", request.BillCycle);

                        if (request.ReportType == SolarReportType.Area && !string.IsNullOrEmpty(request.AreaCode))
                        {
                            logger.Debug("Adding parameter: AreaCode = {AreaCode}", request.AreaCode);
                            cmd.Parameters.AddWithValue("", request.AreaCode);
                        }
                        else if (request.ReportType == SolarReportType.Province && !string.IsNullOrEmpty(request.ProvCode))
                        {
                            logger.Debug("Adding parameter: ProvCode = {ProvCode}", request.ProvCode);
                            cmd.Parameters.AddWithValue("", request.ProvCode);
                        }
                        else if (request.ReportType == SolarReportType.Region && !string.IsNullOrEmpty(request.Region))
                        {
                            logger.Debug("Adding parameter: Region = {Region}", request.Region);
                            cmd.Parameters.AddWithValue("", request.Region);
                        }

                        logger.Debug("Executing query...");
                        using (var reader = cmd.ExecuteReader())
                        {
                            logger.Debug("Reading results...");
                            int recordCount = 0;

                            while (reader.Read())
                            {
                                recordCount++;
                                var solarProgress = MapSolarProgressFromReader(reader);

                                solarProgress.FromArea = ConvertAreaCodeToName(solarProgress.FromArea, areaMapping);
                                solarProgress.ToArea = ConvertAreaCodeToName(solarProgress.ToArea, areaMapping);

                                results.Add(solarProgress);
                            }

                            logger.Info($"Successfully read {recordCount} records from database");
                        }
                    }
                }

                logger.Info("=== END GetDetailedReport (Success) ===");
                logger.Info($"Returning {results.Count} results");
                return results;
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "Database error occurred while fetching detailed report");
                logger.Error($"SQL: {BuildSqlQuery(request)}");
                throw new ApplicationException("Database error occurred while fetching report", ex);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error occurred while fetching detailed report");
                throw;
            }
        }

        private string BuildSqlQuery(SolarProgressRequest request)
        {
            string baseQuery = "SELECT * FROM netmtchg n, areas a, provinces p WHERE n.bill_cycle = ? AND a.area_code = n.area_code AND a.prov_code = p.prov_code";

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery += " AND n.area_code = ?";
                    baseQuery += " ORDER BY a.area_name ASC, n.acc_nbr";
                    break;

                case SolarReportType.Province:
                    baseQuery += " AND a.prov_code = ?";
                    baseQuery += " ORDER BY a.area_name ASC, n.acc_nbr";
                    break;

                case SolarReportType.Region:
                    baseQuery += " AND a.region = ?";
                    baseQuery += " ORDER BY p.prov_name ASC, a.area_name ASC, n.acc_nbr";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery += " ORDER BY a.region ASC, p.prov_name ASC, a.area_name ASC, n.acc_nbr";
                    break;
            }

            return baseQuery;
        }

        private SolarProgressDetailedModel MapSolarProgressFromReader(OleDbDataReader reader)
        {
            try
            {
                // Log available columns for debugging (only in debug mode)
                if (logger.IsDebugEnabled)
                {
                    var availableColumns = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        availableColumns.Add(reader.GetName(i));
                    }
                    logger.Debug($"Available columns: {string.Join(", ", availableColumns)}");
                }

                string rawNetChg = GetColumnValue(reader, "net_chg");
                logger.Debug($"Raw net_chg value: '{rawNetChg}'");

                var model = new SolarProgressDetailedModel
                {
                    Region = GetColumnValue(reader, "region"),
                    Province = GetColumnValue(reader, "prov_name"),
                    Area = GetColumnValue(reader, "area_name"),
                    AccountNumber = GetColumnValue(reader, "acc_nbr"),
                    NetType = MapNetType(rawNetChg),
                    Description = MapDescription(GetColumnValue(reader, "chg_code")),
                    Capacity = GetDecimalValue(reader, "cap_chg"),
                    FromArea = GetFromArea(GetColumnValue(reader, "chg_code"), GetColumnValue(reader, "frm_to_area"), GetColumnValue(reader, "area_name")),
                    ToArea = GetToArea(GetColumnValue(reader, "chg_code"), GetColumnValue(reader, "frm_to_area"), GetColumnValue(reader, "area_name")),
                    FromNetType = GetFromNetType(GetColumnValue(reader, "chg_code"), GetColumnValue(reader, "net_chg")),
                    ToNetType = GetToNetType(GetColumnValue(reader, "chg_code"), GetColumnValue(reader, "net_chg")),
                    ErrorMessage = string.Empty
                };

                logger.Trace($"Mapped record: Account={model.AccountNumber}, NetType={model.NetType}, Capacity={model.Capacity}");

                return model;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping data from database reader");
                throw;
            }
        }

        private Dictionary<string, string> GetAreaNameMapping(OleDbConnection conn)
        {
            var areaMapping = new Dictionary<string, string>();

            try
            {
                string areaQuery = "SELECT area_code, area_name FROM areas";
                logger.Debug($"Loading area mapping with query: {areaQuery}");

                using (var cmd = new OleDbCommand(areaQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    int mappingCount = 0;
                    while (reader.Read())
                    {
                        string areaCode = GetColumnValue(reader, "area_code");
                        string areaName = GetColumnValue(reader, "area_name");

                        if (!string.IsNullOrEmpty(areaCode) && !areaMapping.ContainsKey(areaCode))
                        {
                            areaMapping[areaCode] = areaName;
                            mappingCount++;
                        }
                    }
                    logger.Debug($"Processed {mappingCount} area mappings");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading area name mapping from database");
                throw;
            }

            return areaMapping;
        }

        private string ConvertAreaCodeToName(string areaValue, Dictionary<string, string> areaMapping)
        {
            if (string.IsNullOrEmpty(areaValue))
            {
                logger.Trace("Empty area value provided for conversion");
                return string.Empty;
            }

            bool isNumericCode = true;
            foreach (char c in areaValue)
            {
                if (!char.IsDigit(c))
                {
                    isNumericCode = false;
                    break;
                }
            }

            if (!isNumericCode)
            {
                logger.Trace($"Area value '{areaValue}' is not numeric, returning as-is");
                return areaValue;
            }

            if (areaMapping.TryGetValue(areaValue, out string areaName))
            {
                logger.Trace($"Mapped area code '{areaValue}' to name '{areaName}'");
                return areaName;
            }

            logger.Warn($"Area code '{areaValue}' not found in mapping dictionary");
            return $"{areaValue} (Unknown Area)";
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
                logger.Warn($"Column '{columnName}' not found in result set");
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
                logger.Warn($"Column '{columnName}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column '{columnName}'");
                return 0;
            }
        }





        public static string MapNetType(string netTypeCode)
        {
            if (string.IsNullOrEmpty(netTypeCode))
                return "Unknown";

            string trimmedCode = netTypeCode.Trim();

            switch (trimmedCode)
            {
                case "1":
                case "11":
                    return "Net Metering";
                case "2":
                case "22":
                    return "Net Accounting";
                case "3":
                case "33":
                    return "Net Plus";
                case "4":
                case "44":
                    return "Net Plus Plus";
                case "21":
                    return "Net Metering";
                default:
                    return "Net Accounting";
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

        private string GetFromArea(string changeCode, string fromToArea, string currentAreaName)
        {
            // parameter renamed from fromToAreaName to fromToArea
            if (string.IsNullOrEmpty(changeCode))
                return string.Empty;

            string trimmedCode = changeCode.Trim();

            switch (trimmedCode)
            {
                case "F":
                    return fromToArea ?? string.Empty;
                case "T":
                    return currentAreaName ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        private string GetToArea(string changeCode, string fromToArea, string currentAreaName)
        {
            // parameter renamed from fromToAreaName to fromToArea
            if (string.IsNullOrEmpty(changeCode))
                return string.Empty;

            string trimmedCode = changeCode.Trim();

            switch (trimmedCode)
            {
                case "F":
                    return currentAreaName ?? string.Empty;
                case "T":
                    return fromToArea ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        private string GetFromNetType(string changeCode, string netChange)
        {
            if (changeCode != "Y" || string.IsNullOrEmpty(netChange) || netChange.Length < 2)
                return string.Empty;

            return MapNetType(netChange.Trim().Substring(0, 1));
        }

        private string GetToNetType(string changeCode, string netChange)
        {
            if (changeCode != "Y" || string.IsNullOrEmpty(netChange) || netChange.Length < 2)
                return string.Empty;

            return MapNetType(netChange.Trim().Substring(1, 1));
        }
    }

}