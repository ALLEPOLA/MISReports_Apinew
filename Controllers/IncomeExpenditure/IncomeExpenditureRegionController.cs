using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/incomeexpenditureregion")]
    public class IncomeExpenditureRegionController : ApiController
    {
        private readonly IncomeExpenditureRegionRepository _repository = new IncomeExpenditureRegionRepository();

        // One endpoint: companyId + year + month
        [HttpGet]
        [Route("{companyId}/{repyear}/{repmonth}")]
        public IHttpActionResult GetIncomeExpenditureRegion(string companyId, string repyear, string repmonth)
        {
            try
            {
                var result = _repository.GetIncomeExpenditureRegion(companyId.Trim(), repyear.Trim(), repmonth.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Income vs Expenditure Region data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
