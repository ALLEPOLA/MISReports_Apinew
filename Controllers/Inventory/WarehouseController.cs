using MISReports_Api.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/warehouse")]
    public class WarehouseController : ApiController
    {
        private readonly WarehouseRepository _repository = new WarehouseRepository();

        // Endpoint: GET /api/warehouse/{epfNo}
        [HttpGet]
        [Route("{epfNo}")]
        public IHttpActionResult GetWarehousesByEpf(string epfNo)
        {
            try
            {
                var result = _repository.GetWarehousesByEpf(epfNo.Trim());

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
                    errorMessage = "Cannot get Warehouses.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
