// Controllers/BillCycleController.cs
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/billcycle")]
    public class BillCycleController : ApiController
    {
        private readonly BillCycleRepository _repository = new BillCycleRepository();

        [HttpGet]
        [Route("max")]
        public IHttpActionResult GetMaxBillCycle()
        {
            try
            {
                var result = _repository.GetLast24BillCycles();

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("customerType/{custType}")]
        public IHttpActionResult GetCustomerTypeDescription(string custType)
        {
            try
            {
                var result = _repository.GetCustomerTypeDescription(custType);

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get customer type description",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}