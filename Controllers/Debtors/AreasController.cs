// Controllers/AreasController.cs
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/areas")]
    public class AreasController : ApiController
    {
        private readonly AreasRepository _areasRepository = new AreasRepository();

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAreas()
        {
            try
            {
                var areas = _areasRepository.GetAreas();

                return Ok(JObject.FromObject(new
                {
                    data = areas,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas data.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}