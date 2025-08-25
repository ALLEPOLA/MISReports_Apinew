using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/debtors")]
    public class DebtorController : ApiController
    {
        private readonly DebtorRepository _repository = new DebtorRepository();

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetDebtorDetails(
            [FromUri] string custType,
            [FromUri] string billCycle,
            [FromUri] string areaCode,
            [FromUri] string ageRange = "All")
        {
            if (string.IsNullOrWhiteSpace(custType))
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Customer type parameter is required."
                }));
            }

            if (string.IsNullOrWhiteSpace(billCycle))
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Bill cycle parameter is required."
                }));
            }

            if (string.IsNullOrWhiteSpace(areaCode))
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Area code parameter is required."
                }));
            }

            try
            {
                var request = new DebtorRequest
                {
                    CustType = custType,
                    BillCycle = billCycle,
                    AreaCode = areaCode,
                    AgeRange = ParseAgeRange(ageRange)
                };

                var debtors = _repository.GetDebtorDetails(request);

                return Ok(JObject.FromObject(new
                {
                    data = debtors,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get debtor details.",
                    errorDetails = ex.Message
                }));
            }
        }

        private AgeRange ParseAgeRange(string ageRange)
        {
            if (Enum.TryParse(ageRange, true, out AgeRange result))
                return result;
            return AgeRange.All;
        }
    }
}
