using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;


namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/incomeexpenditure")]
    public class IncomeExpenditureController : ApiController

    {
        private readonly IncomeExpenditureRepository _repository = new IncomeExpenditureRepository();
        // one  company details 
        [HttpGet]
        [Route("{costctr}/{repyear}/{repmonth}")]
        public IHttpActionResult GetIncomeExpenditure(string costctr, string repyear, string repmonth)
        {
            try
            {
                var result = _repository.GetIncomeExpenditure(costctr.Trim(), repyear.Trim(), repmonth.Trim());

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
                    errorMessage = "Cannot get Income over Expenditure data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
        //get department  User wise
        [HttpGet]
        [Route("departments/{epfno}")]
        public IHttpActionResult GetDepartmentsByUser(string epfno)
        {
            try
            {
                var result = _repository.GetDepartmentsByUser(epfno.Trim());

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
                    errorMessage = "Cannot get department data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        //get companys user level wise
        [HttpGet]
        [Route("Usercompanies/{epfno}/{lvl_no}")]

        public IHttpActionResult GetCompaniesByUserLevel(string epfno, string lvl_no)
        {
            try
            {
                var result = _repository.GetCompaniesByUserlevel(epfno.Trim(), lvl_no.Trim());

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
                    errorMessage = "Cannot get company list for the user.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}