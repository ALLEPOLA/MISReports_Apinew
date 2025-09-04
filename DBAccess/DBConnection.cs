// DBConnection.cs
using System;
using System.Configuration;
using System.Data.OleDb;

namespace MISReports_Api.DBAccess
{
    public class DBConnection
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixBulkConnection"].ConnectionString;

        public bool TestConnection(out string errorMessage)
        {
            try
            {
                using (var conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    errorMessage = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public OleDbConnection GetConnection()
        {
            return new OleDbConnection(connectionString);
        }

        public string ConnectionString => connectionString;
    }
}