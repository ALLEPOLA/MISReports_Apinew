using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/inventoryaverageconsumption")]
    public class InventoryAverageConsumptionController : ApiController
    {
        private readonly InventoryAverageConsumptionRepository _repository = new InventoryAverageConsumptionRepository();

        [HttpGet]
        [Route("report/{costCenter}/{warehouseCode}/{fromDate}/{toDate}")]
        public IHttpActionResult GetAverageConsumption(string costCenter, string warehouseCode, string fromDate, string toDate)
        {
            try
            {
                // Validate parameters
                if (string.IsNullOrWhiteSpace(costCenter) || string.IsNullOrWhiteSpace(warehouseCode))
                {
                    return BadRequest("Cost center and warehouse code cannot be empty");
                }

                DateTime parsedFromDate;
                DateTime parsedToDate;

                if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedFromDate) ||
                    !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedToDate))
                {
                    return BadRequest("Invalid date format. Please use yyyyMMdd format");
                }

                var result = _repository.GetAverageConsumption(
                    costCenter.Trim(),
                    warehouseCode.Trim(),
                    parsedFromDate,
                    parsedToDate);

                var response = new InventoryAverageConsumptionResponse
                {
                    Data = result,
                    ErrorMessage = null,
                    ErrorDetails = null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAverageConsumption: {ex}");
                var errorResponse = new InventoryAverageConsumptionResponse
                {
                    Data = null,
                    ErrorMessage = "Cannot get average consumption data.",
                    ErrorDetails = ex.Message
                };

                return Ok(errorResponse);
            }
        }

        [HttpGet]
        [Route("warehouses/{epfNo}")]
        public IHttpActionResult GetWarehousesByEpfNo(string epfNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(epfNo))
                {
                    return BadRequest("EPF number cannot be empty");
                }

                var result = _repository.GetWarehousesByEpfNo(epfNo.Trim());

                var response = new WarehouseResponse
                {
                    Data = result,
                    ErrorMessage = null,
                    ErrorDetails = null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetWarehousesByEpfNo: {ex}");
                var errorResponse = new WarehouseResponse
                {
                    Data = null,
                    ErrorMessage = "Cannot get warehouse data.",
                    ErrorDetails = ex.Message
                };

                return Ok(errorResponse);
            }
        }
    }
}