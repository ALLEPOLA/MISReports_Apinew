using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/regionwisetrial")]
    public class RegionwiseTrialController : ApiController
    {
        private readonly RegionwiseTrialRepository _repository = new RegionwiseTrialRepository();

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetRegionwiseTrial([FromUri] string companyId, [FromUri] string month, [FromUri] string year)
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

                var data = _repository.GetRegionwiseTrialData(companyId, month, year);

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data = data
                });
            }
            catch (Exception ex)
            {
                // Log error details
                System.Diagnostics.Trace.WriteLine($"ERROR: {ex.ToString()}");

                return Ok(new
                {
                    success = false,
                    message = "Error retrieving regionwise trial balance data",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}