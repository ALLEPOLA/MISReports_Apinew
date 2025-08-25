using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/materials")]
    public class MaterialController : ApiController
    {
        private readonly MaterialRepository _materialRepository = new MaterialRepository();

        // Get name and mat_cd
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetMaterials()
        {
            try
            {
                var materials = _materialRepository.GetActiveMaterials();

                var response = new
                {
                    data = materials,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get material details.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        // Get region-wise stock details
        [HttpGet]
        [Route("stocks")]
        public IHttpActionResult GetMaterialStocks()
        {
            try
            {
                var stocks = _materialRepository.GetMaterialStocks();

                var response = new
                {
                    data = stocks,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get region-wise data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        // Get all stock balances for a given material code
        [HttpGet]
        [Route("stock-balances")]
        public IHttpActionResult GetMaterialStockBalances([FromUri] string matCd)
        {
            if (string.IsNullOrWhiteSpace(matCd))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Material code is required."
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }

            try
            {
                var balances = _materialRepository.GetMaterialStockBalances(matCd);

                var response = new
                {
                    data = balances,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get stock balance details.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        // Get region-wise stock by material code
        [HttpGet]
        [Route("stocks/by-matcd/{matCd}")]
        public IHttpActionResult GetMaterialStocksByMatCd([FromUri] string matCd)
        {
            if (string.IsNullOrWhiteSpace(matCd))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Material code is required."
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }

            try
            {
                var stocks = _materialRepository.GetMaterialStocksByMatCd(matCd);

                var response = new
                {
                    data = stocks,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Failed to retrieve stock data for material.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        //  Get province-wise stock by material code
        [HttpGet]
        [Route("stocks/by-matcd-province-wise/{matCd}")]
        public IHttpActionResult GetMaterialStocksByMatCdProvinceWise([FromUri] string matCd)
        {
            if (string.IsNullOrWhiteSpace(matCd))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Material code is required."
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }

            try
            {
                var stocks = _materialRepository.GetMaterialStocksByMatCdProvinceWise(matCd);

                var response = new
                {
                    data = stocks,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Failed to retrieve province-wise stock data for material.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        
        
    }
}
