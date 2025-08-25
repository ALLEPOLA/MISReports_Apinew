// DAL/BillCycleRepository.cs
using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL
{
    public class BillCycleRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixConnection"].ConnectionString;

        public BillCycleModel GetLast24BillCycles()
        {
            var model = new BillCycleModel();

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Get max bill cycle as integer
                    string sql = "SELECT max(bill_cycle) FROM ageacct";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj != null && maxCycleObj != DBNull.Value)
                        {
                            int maxCycle;
                            if (int.TryParse(maxCycleObj.ToString(), out maxCycle))
                            {
                                model.MaxBillCycle = maxCycle.ToString();
                                model.BillCycles = Generate24MonthYearStrings(maxCycle);
                            }
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    // Log error appropriately for .NET 4.7.2
                    System.Diagnostics.Trace.WriteLine($"Error retrieving max bill cycle: {ex.Message}");
                    model.ErrorMessage = "Error retrieving max bill cycle";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Unexpected error: {ex.Message}");
                    model.ErrorMessage = "Unexpected error occurred";
                }
            }

            return model;
        }

        private List<string> Generate24MonthYearStrings(int maxCycle)
        {
            List<string> monthYearStrings = new List<string>();

            for (int i = maxCycle; i > maxCycle - 24 && i > 0; i--)
            {
                monthYearStrings.Add(ConvertToMonthYear(i));
            }

            return monthYearStrings;
        }

        private string ConvertToMonthYear(int billCycle)
        {
            try
            {
                int mnth = (billCycle - 100) % 12;
                int m_mnth = mnth;
                int yr = 97 + (billCycle - 100) / 12;
                string yr1;

                if (mnth == 0)
                {
                    yr -= 1;
                    m_mnth = 12;
                }

                yr1 = (yr % 100).ToString("00");

                switch (m_mnth)
                {
                    case 1: return "Jan " + yr1;
                    case 2: return "Feb " + yr1;
                    case 3: return "Mar " + yr1;
                    case 4: return "Apr " + yr1;
                    case 5: return "May " + yr1;
                    case 6: return "Jun " + yr1;
                    case 7: return "Jul " + yr1;
                    case 8: return "Aug " + yr1;
                    case 9: return "Sep " + yr1;
                    case 10: return "Oct " + yr1;
                    case 11: return "Nov " + yr1;
                    case 12: return "Dec " + yr1;
                    default: return "Unknown";
                }
            }
            catch
            {
                return "Invalid";
            }
        }

        public CustomerTypeModel GetCustomerTypeDescription(string custType)
        {
            var model = new CustomerTypeModel { CustType = custType };

            if (string.IsNullOrEmpty(custType))
            {
                model.Description = "Unknown";
                return model;
            }

            switch (custType.ToUpper())
            {
                case "A":
                    model.Description = "Debtors Age Analysis – Active Customers";
                    break;
                case "G":
                    model.Description = "Debtors Age Analysis – Government Customers";
                    break;
                case "F":
                    model.Description = "Debtors Age Analysis – Finalized Customers";
                    break;
                default:
                    model.Description = "Debtors Age Analysis – " + custType + " Customers";
                    break;
            }

            return model;
        }
    }
}