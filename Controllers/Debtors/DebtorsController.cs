using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/debtors")]
    public class DebtorsController : ApiController
    {
        private readonly DebtorsRepository _debtorsRepository = new DebtorsRepository();

        // GET: api/debtors/summary
        [HttpGet]
        [Route("summary")]
        public IHttpActionResult GetDebtorsSummary([FromUri] string opt, [FromUri] string cycle, [FromUri] string areaCode = null)


        {
            if (string.IsNullOrWhiteSpace(opt))
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Option parameter is required."
                }));
            }

            if (string.IsNullOrWhiteSpace(cycle))
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cycle parameter is required."
                }));
            }

            if (!IsValidOption(opt))
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Invalid option. Valid options are: A, P, D, E."
                }));
            }

            if ((opt.ToUpper() == "A" || opt.ToUpper() == "P" || opt.ToUpper() == "D") && string.IsNullOrWhiteSpace(areaCode))
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Area code parameter is required for the selected option."
                }));
            }

            try
            {
                var debtors = _debtorsRepository.GetDebtorsData(opt, cycle, areaCode);

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
                    errorMessage = "Cannot get debtors summary.",
                    errorDetails = ex.Message
                }));
            }
        }

        private bool IsValidOption(string opt)
        {
            var validOptions = new[] { "A", "P", "D", "E" };
            return !string.IsNullOrWhiteSpace(opt) && Array.Exists(validOptions, o => o.Equals(opt.ToUpper()));
        }
    }
}
