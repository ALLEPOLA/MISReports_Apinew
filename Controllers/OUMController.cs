using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/oum")]
    public class OUMController : ApiController
    {
        [HttpPost]
        [Route("read-excel")]
        public async Task<IHttpActionResult> ReadExcelFile()
        {
            try
            {
                if (!Request.Content.IsMimeMultipartContent())
                {
                    return Ok(JObject.FromObject(new
                    {
                        success = false,
                        message = "Invalid request format. Please use multipart/form-data.",
                        data = (object)null,
                        totalRecords = 0
                    }));
                }

                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                // Case-insensitive search for file field - supports "file", "File", "FILE", etc.
                var fileContent = provider.Contents.FirstOrDefault(x =>
                    string.Equals(x.Headers.ContentDisposition?.Name?.Trim('"'), "file", StringComparison.OrdinalIgnoreCase));

                // If not found with "file" name, look for any field with a filename (fallback)
                if (fileContent == null)
                {
                    fileContent = provider.Contents.FirstOrDefault(x =>
                        !string.IsNullOrEmpty(x.Headers.ContentDisposition?.FileName?.Trim('"')));
                }

                // If still not found, try common field names
                if (fileContent == null)
                {
                    fileContent = provider.Contents.FirstOrDefault(x =>
                    {
                        var name = x.Headers.ContentDisposition?.Name?.Trim('"')?.ToLower();
                        return name == "upload" || name == "attachment" || name == "document" || name == "excel";
                    });
                }

                if (fileContent == null)
                {
                    // Debug info to help identify the issue
                    var debugInfo = new List<string>();
                    foreach (var content in provider.Contents)
                    {
                        var name = content.Headers.ContentDisposition?.Name?.Trim('"');
                        var filename = content.Headers.ContentDisposition?.FileName?.Trim('"');
                        debugInfo.Add($"Name: '{name}', FileName: '{filename}'");
                    }

                    return Ok(JObject.FromObject(new
                    {
                        success = false,
                        message = "No file found in the request.",
                        data = (object)null,
                        totalRecords = 0,
                        debug = debugInfo,
                        totalParts = provider.Contents.Count,
                        hint = "Make sure you're uploading a file with field name 'file' or any field with a filename"
                    }));
                }

                var fileBytes = await fileContent.ReadAsByteArrayAsync();
                var fileName = fileContent.Headers.ContentDisposition.FileName?.Trim('"');

                if (fileBytes.Length == 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        success = false,
                        message = "File is empty.",
                        data = (object)null,
                        totalRecords = 0
                    }));
                }

                if (!string.IsNullOrEmpty(fileName) && !fileName.ToLower().EndsWith(".xlsx") && !fileName.ToLower().EndsWith(".xls"))
                {
                    return Ok(JObject.FromObject(new
                    {
                        success = false,
                        message = "Invalid file format. Please upload an Excel file (.xlsx or .xls).",
                        data = (object)null,
                        totalRecords = 0,
                        receivedFileName = fileName
                    }));
                }

                var employeeData = new List<OUMEmployeeModel>();

                try
                {
                    using (var stream = new MemoryStream(fileBytes))
                    using (var package = new ExcelPackage(stream))
                    {
                        System.Diagnostics.Debug.WriteLine("ExcelPackage created successfully");

                        // Check if workbook exists
                        if (package.Workbook == null)
                        {
                            return Ok(JObject.FromObject(new
                            {
                                success = false,
                                message = "Could not access workbook. The file might be corrupted or not a valid Excel file.",
                                data = (object)null,
                                totalRecords = 0,
                                fileSize = fileBytes.Length,
                                fileName = fileName
                            }));
                        }

                        System.Diagnostics.Debug.WriteLine("Workbook accessed successfully");

                        // Check worksheet collection
                        if (package.Workbook.Worksheets == null)
                        {
                            return Ok(JObject.FromObject(new
                            {
                                success = false,
                                message = "Worksheets collection is null.",
                                data = (object)null,
                                totalRecords = 0
                            }));
                        }

                        System.Diagnostics.Debug.WriteLine($"Worksheet count: {package.Workbook.Worksheets.Count}");

                        if (package.Workbook.Worksheets.Count == 0)
                        {
                            return Ok(JObject.FromObject(new
                            {
                                success = false,
                                message = "Excel file contains no worksheets.",
                                data = (object)null,
                                totalRecords = 0,
                                fileSize = fileBytes.Length,
                                fileName = fileName
                            }));
                        }

                        // Try to access the first worksheet safely
                        ExcelWorksheet worksheet = null;
                        try
                        {
                            worksheet = package.Workbook.Worksheets[0];
                            System.Diagnostics.Debug.WriteLine($"Got first worksheet: {worksheet?.Name ?? "Unnamed"}");
                        }
                        catch (Exception wsEx)
                        {
                            return Ok(JObject.FromObject(new
                            {
                                success = false,
                                message = $"Error accessing first worksheet: {wsEx.Message}",
                                data = (object)null,
                                totalRecords = 0,
                                worksheetCount = package.Workbook.Worksheets.Count
                            }));
                        }

                        if (worksheet == null)
                        {
                            return Ok(JObject.FromObject(new
                            {
                                success = false,
                                message = "First worksheet is null.",
                                data = (object)null,
                                totalRecords = 0
                            }));
                        }

                        if (worksheet.Dimension == null)
                        {
                            return Ok(JObject.FromObject(new
                            {
                                success = false,
                                message = "Excel worksheet is empty (no data range found).",
                                data = (object)null,
                                totalRecords = 0,
                                worksheetName = worksheet.Name
                            }));
                        }

                        System.Diagnostics.Debug.WriteLine($"Worksheet dimensions: {worksheet.Dimension.Address}");

                        string firstCellValue = "";
                        try
                        {
                            firstCellValue = worksheet.Cells[1, 1].Value?.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"First cell value: '{firstCellValue}'");
                        }
                        catch (Exception cellEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error reading first cell: {cellEx.Message}");
                        }

                        int startRow = firstCellValue.ToLower().Contains("auth date") ? 2 : 1;
                        System.Diagnostics.Debug.WriteLine($"Start row: {startRow}, End row: {worksheet.Dimension.End.Row}");

                        for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
                        {
                            try
                            {
                                var cellValue = worksheet.Cells[row, 1].Value?.ToString();
                                if (string.IsNullOrWhiteSpace(cellValue))
                                    continue;

                                System.Diagnostics.Debug.WriteLine($"Processing row {row}");

                                var employee = new OUMEmployeeModel
                                {
                                    AuthDate = DateTime.TryParse(worksheet.Cells[row, 1].Value?.ToString(), out var authDate) ? authDate : DateTime.MinValue,
                                    OrderId = int.TryParse(worksheet.Cells[row, 2].Value?.ToString(), out var orderId) ? orderId : 0,
                                    AcctNumber = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "",
                                    BankCode = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "",
                                    BillAmt = decimal.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out var billAmt) ? billAmt : 0m,
                                    TaxAmt = decimal.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out var taxAmt) ? taxAmt : 0m,
                                    TotAmt = decimal.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out var totAmt) ? totAmt : 0m,
                                    AuthCode = worksheet.Cells[row, 8].Value?.ToString()?.Trim() ?? "",
                                    CardNo = worksheet.Cells[row, 9].Value?.ToString()?.Trim() ?? ""
                                };

                                employeeData.Add(employee);
                            }
                            catch (Exception rowEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error processing row {row}: {rowEx.Message}");
                                // Continue processing other rows
                            }
                        }
                    }
                }
                catch (Exception excelEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Excel processing error: {excelEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {excelEx.StackTrace}");
                    return Ok(JObject.FromObject(new
                    {
                        success = false,
                        message = $"Error reading Excel file: {excelEx.Message}",
                        data = (object)null,
                        totalRecords = 0,
                        innerError = excelEx.InnerException?.Message,
                        stackTrace = excelEx.StackTrace,
                        fileSize = fileBytes?.Length ?? 0,
                        fileName = fileName
                    }));
                }

                return Ok(JObject.FromObject(new
                {
                    success = employeeData.Count > 0,
                    message = employeeData.Count > 0
                        ? $"Successfully read {employeeData.Count} records from Excel file"
                        : "No data found in Excel file",
                    data = employeeData,
                    totalRecords = employeeData.Count,
                    fileName = fileName
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    success = false,
                    message = $"Error processing Excel file: {ex.Message}",
                    data = (object)null,
                    totalRecords = 0,
                    stackTrace = ex.StackTrace
                }));
            }
        }
    }
}