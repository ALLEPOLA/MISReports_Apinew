using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/trialbalance")]
    public class CompanyTrialBalanceController : ApiController
    {
        private readonly CompanyTrialBalanceRepository _repository = new CompanyTrialBalanceRepository();

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetTrialBalance([FromUri] string companyId, [FromUri] string month, [FromUri] string year)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(companyId))
                {
                    return BadRequest("Company ID is required");
                }

                if (string.IsNullOrWhiteSpace(month))
                {
                    return BadRequest("Month is required");
                }

                if (string.IsNullOrWhiteSpace(year))
                {
                    return BadRequest("Year is required");
                }

                // Log input parameters
                System.Diagnostics.Trace.WriteLine($"Request received: companyId={companyId}, month={month}, year={year}");

                var data = _repository.GetTrialBalanceData(companyId, month, year);

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data = data
                });
            }
            catch (Exception ex)
            {
                //  error details
                System.Diagnostics.Trace.WriteLine($"ERROR: {ex.ToString()}");

                return Ok(new
                {
                    success = false,
                    message = "Error retrieving trial balance data",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}