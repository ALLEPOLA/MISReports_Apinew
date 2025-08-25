using MISReports_Api.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/userrole")]
    public class UserRoleController : ApiController
    {
        private readonly UserRoleRepository _repository = new UserRoleRepository();

        [HttpGet]
        [Route("{epfNo}")]
        public IHttpActionResult GetUserRole(string epfNo)
        {
            try
            {
                var result = _repository.GetUserRole(epfNo.Trim());

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
                    errorMessage = "Cannot get User Role.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
