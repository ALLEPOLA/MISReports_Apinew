using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class UserRoleRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<UserRoleModel> GetUserRole(string epfNo)
        {
            var roles = new List<UserRoleModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT UPPER(r.roleid) AS RoleId, r.USER_GROUP 
                                   FROM rep_role_new r 
                                   WHERE r.epf_no = :epf_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("epf_no", epfNo);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                roles.Add(new UserRoleModel
                                {
                                    RoleId = reader["RoleId"]?.ToString(),
                                    UserGroup = reader["USER_GROUP"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetUserRole: {ex.Message}");
                throw;
            }

            return roles;
        }
    }
}
