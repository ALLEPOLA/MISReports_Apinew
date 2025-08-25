using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provinceincomeexpenditure")]
    public class ProvinceIncomeExpenditureController : ApiController
    {
        private readonly ProvinceIncomeExpenditureRepository _repository = new ProvinceIncomeExpenditureRepository();

        [HttpGet]
        [Route("{compId}/{repYear}/{repMonth}")]
        public IHttpActionResult GetProvinceIncomeExpenditure(string compId, string repYear, string repMonth)
        {
            try
            {
                Debug.WriteLine($"API Request started: compId={compId}, repYear={repYear}, repMonth={repMonth}");

                // Validate parameters
                if (string.IsNullOrWhiteSpace(compId)) throw new ArgumentException("Company ID is required");
                if (string.IsNullOrWhiteSpace(repYear)) throw new ArgumentException("Year is required");
                if (string.IsNullOrWhiteSpace(repMonth)) throw new ArgumentException("Month is required");

                // Additional validation for year and month format
                if (!int.TryParse(repYear, out int year) || year < 1900 || year > 2100)
                    throw new ArgumentException("Invalid year format");

                if (!int.TryParse(repMonth, out int month) || month < 1 || month > 12)
                    throw new ArgumentException("Invalid month format (1-12)");

                var result = _repository.GetProvinceIncomeExpenditure(
                    compId.Trim().ToUpper(),  // Ensure consistent case
                    repYear.Trim(),
                    repMonth.Trim());

                Debug.WriteLine($"API Request completed. Found {result.Count} records");

                var response = new
                {
                    success = true,
                    data = result,
                    recordCount = result.Count,
                    parameters = new { compId, repYear, repMonth },
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (ArgumentException argEx)
            {
                Debug.WriteLine($"API Argument Error: {argEx.Message}");

                var errorResponse = new
                {
                    success = false,
                    data = new List<ProvinceIncomeExpenditureModel>(),
                    recordCount = 0,
                    errorMessage = argEx.Message,
                    errorType = "ValidationError"
                };

                return BadRequest(JsonConvert.SerializeObject(errorResponse));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API Error: {ex.Message}\n{ex.StackTrace}");

                var errorResponse = new
                {
                    success = false,
                    data = new List<ProvinceIncomeExpenditureModel>(),
                    recordCount = 0,
                    errorMessage = "Cannot get Province Income over Expenditure data.",
                    errorDetails = ex.Message,
                    errorType = "SystemError",
                    timestamp = DateTime.Now
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

    }
}