// DAL/OUMRepository.cs
/*using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;

namespace MISReports_Api.DAL
{
    public class OUMRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixCreditCard"].ConnectionString;

        public int InsertIntoAmex2(List<OUMEmployeeModel> data)
        {
            int count = 0;
            using (var conn = new OdbcConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Clear existing data from test_amex2
                    using (var cmd = new OdbcCommand("DELETE FROM test_amex2", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert new data
                    foreach (var item in data)
                    {
                        using (var cmd = new OdbcCommand())
                        {
                            cmd.Connection = conn;
                            cmd.CommandText = @"INSERT INTO test_amex2 (pdate, o_id, acct_no, cname, bill_amt, tax, tot_amt, authcode, cno) 
                                              VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                            cmd.Parameters.AddWithValue("pdate", item.AuthDate);
                            cmd.Parameters.AddWithValue("o_id", item.OrderId);
                            cmd.Parameters.AddWithValue("acct_no", item.AcctNumber ?? "");
                            cmd.Parameters.AddWithValue("cname", item.BankCode ?? "");
                            cmd.Parameters.AddWithValue("bill_amt", item.BillAmt);
                            cmd.Parameters.AddWithValue("tax", item.TaxAmt);
                            cmd.Parameters.AddWithValue("tot_amt", item.TotAmt);
                            cmd.Parameters.AddWithValue("authcode", item.AuthCode ?? "");
                            cmd.Parameters.AddWithValue("cno", item.CardNo ?? "");

                            count += cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (OdbcException ex)
                {
                    Console.WriteLine($"Error inserting into Amex2: {ex.Message}");
                    throw new Exception($"Database error while inserting records: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error inserting into Amex2: {ex.Message}");
                    throw new Exception($"Unexpected error while inserting records: {ex.Message}", ex);
                }
            }
            return count;
        }

        public void RefreshCrdTemp()
        {
            using (var conn = new OdbcConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Delete existing records from test_crdt_tmp
                    using (var cmd = new OdbcCommand("DELETE FROM test_crdt_tmp", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert from test_amex2 to test_crdt_tmp
                    string insertSql = @"INSERT INTO test_crdt_tmp 
                                       (order_id, acct_number, custname, username, bill_amt, tax_amt, tot_amt, trstatus,
                                        authcode, pmnt_date, auth_date, cebres, serl_no, bank_code, bran_code, inst_status, 
                                        updt_status, updt_flag, post_flag, err_flag, post_date, card_no, payment_type, 
                                        ref_number, reference_type, sms_st)
                                       SELECT o_id, acct_no, '-', '-', bill_amt, tax, tot_amt, 'S',
                                              authcode, pdate, pdate, 'S', 0, cname, 'CRC', '', '', '', '', '', null,
                                              cno, 'Bil', acct_no, 'RSK', ''
                                       FROM test_amex2";

                    using (var cmd = new OdbcCommand(insertSql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Update null values for specific fields
                    using (var cmd = new OdbcCommand(@"UPDATE test_crdt_tmp 
                                                     SET updt_flag = NULL, post_flag = NULL, err_flag = NULL, sms_st = NULL 
                                                     WHERE updt_flag = '' OR post_flag = '' OR err_flag = '' OR sms_st = ''", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Update payment_type where ref_number is longer than 10 characters  
                    using (var cmd = new OdbcCommand(@"UPDATE test_crdt_tmp 
                                                     SET payment_type = 'PIV' 
                                                     WHERE LENGTH(ref_number) > 10", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (OdbcException ex)
                {
                    Console.WriteLine($"Error refreshing CrdTemp: {ex.Message}");
                    throw new Exception($"Database error while refreshing CrdTemp table: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error refreshing CrdTemp: {ex.Message}");
                    throw new Exception($"Unexpected error while refreshing CrdTemp table: {ex.Message}", ex);
                }
            }
        }

        public List<OUMCrdTempModel> GetCrdTempRecords()
        {
            var records = new List<OUMCrdTempModel>();
            using (var conn = new OdbcConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT * FROM test_crdt_tmp ORDER BY auth_date";

                    using (var cmd = new OdbcCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var record = new OUMCrdTempModel
                            {
                                OrderId = GetSafeInt(reader, "order_id"),
                                AcctNumber = GetSafeString(reader, "acct_number"),
                                CustName = GetSafeString(reader, "custname"),
                                UserName = GetSafeString(reader, "username"),
                                BillAmt = GetSafeDecimal(reader, "bill_amt"),
                                TaxAmt = GetSafeDecimal(reader, "tax_amt"),
                                TotAmt = GetSafeDecimal(reader, "tot_amt"),
                                TrStatus = GetSafeString(reader, "trstatus"),
                                AuthCode = GetSafeString(reader, "authcode"),
                                PmntDate = GetSafeDateTime(reader, "pmnt_date"),
                                AuthDate = GetSafeDateTime(reader, "auth_date"),
                                CebRes = GetSafeString(reader, "cebres"),
                                SerlNo = GetSafeInt(reader, "serl_no"),
                                BankCode = GetSafeString(reader, "bank_code"),
                                BranCode = GetSafeString(reader, "bran_code"),
                                InstStatus = GetSafeString(reader, "inst_status"),
                                UpdtStatus = GetSafeString(reader, "updt_status"),
                                UpdtFlag = GetSafeString(reader, "updt_flag"),
                                PostFlag = GetSafeString(reader, "post_flag"),
                                ErrFlag = GetSafeString(reader, "err_flag"),
                                PostDate = GetSafeNullableDateTime(reader, "post_date"),
                                CardNo = GetSafeString(reader, "card_no"),
                                PaymentType = GetSafeString(reader, "payment_type"),
                                RefNumber = GetSafeString(reader, "ref_number"),
                                ReferenceType = GetSafeString(reader, "reference_type"),
                                SmsSt = GetSafeString(reader, "sms_st")
                            };
                            records.Add(record);
                        }
                    }
                }
                catch (OdbcException ex)
                {
                    Console.WriteLine($"Error retrieving CrdTemp records: {ex.Message}");
                    throw new Exception($"Database error while retrieving records: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error retrieving CrdTemp records: {ex.Message}");
                    throw new Exception($"Unexpected error while retrieving records: {ex.Message}", ex);
                }
            }
            return records;
        }

        public bool ApproveRecords()
        {
            using (var conn = new OdbcConnection(connectionString))
            {
                OdbcTransaction transaction = null;
                try
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    // Insert into test_crdtcdslt (production table)
                    using (var cmd = new OdbcCommand("INSERT INTO test_crdtcdslt SELECT * FROM test_crdt_tmp", conn, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert into backup table (with proper schema reference)
                    using (var cmd = new OdbcCommand("INSERT INTO test_crdt_tmp_backup SELECT * FROM test_crdt_tmp", conn, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Delete from temp table
                    using (var cmd = new OdbcCommand("DELETE FROM test_crdt_tmp", conn, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
                catch (OdbcException ex)
                {
                    transaction?.Rollback();
                    Console.WriteLine($"Error approving records: {ex.Message}");
                    throw new Exception($"Database error while approving records: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    transaction?.Rollback();
                    Console.WriteLine($"Unexpected error approving records: {ex.Message}");
                    throw new Exception($"Unexpected error while approving records: {ex.Message}", ex);
                }
            }
        }

        // Helper methods for safe data conversion
        private string GetSafeString(OdbcDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal)?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private int GetSafeInt(OdbcDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
            }
            catch
            {
                return 0;
            }
        }

        private decimal GetSafeDecimal(OdbcDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch
            {
                return 0m;
            }
        }

        private DateTime GetSafeDateTime(OdbcDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? DateTime.MinValue : Convert.ToDateTime(reader.GetValue(ordinal));
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private DateTime? GetSafeNullableDateTime(OdbcDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal));
            }
            catch
            {
                return null;
            }
        }
    }
}
*/