using MISReports_Api.DAL.SolarProgressClarification;
using MISReports_Api.DAL.Shared;
using MISReports_Api.Models;
using MISReports_Api.Models.SolarInformation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("bulkapi")]
    public class SolarController : ApiController
    {
        private readonly AreasDao _areasDao = new AreasDao();
        private readonly ProvinceDao _provinceDao = new ProvinceDao();
        private readonly RegionDao _regionDao = new RegionDao();
        private readonly BillCycleDao _billCycleDao = new BillCycleDao();
        private readonly DetailedDao _detailedDao = new DetailedDao();
        private readonly SummaryDao _summaryDao = new SummaryDao();

        [HttpGet]
        [Route("areas")]
        public IHttpActionResult GetAreas()
        {
            try
            {
                if (!_areasDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var areas = _areasDao.GetAreas();

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

        [HttpGet]
        [Route("province")]
        public IHttpActionResult GetProvince()
        {
            try
            {
                if (!_provinceDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var province = _provinceDao.GetProvince();

                return Ok(JObject.FromObject(new
                {
                    data = province,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get province data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("region")]
        public IHttpActionResult GetRegion()
        {
            try
            {
                if (!_regionDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var region = _regionDao.GetRegion();

                return Ok(JObject.FromObject(new
                {
                    data = region,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get region data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("billcycle/max")]
        public IHttpActionResult GetMaxBillCycle()
        {
            try
            {
                var result = _billCycleDao.GetLast24BillCycles();

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
        [Route("solar-progress/detailed")]
        public IHttpActionResult GetDetailedReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessDetailedRequest(request);
        }

        [HttpGet]
        [Route("solar-progress/summary")]
        public IHttpActionResult GetSummaryReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessSummaryRequest(request);
        }

        private IHttpActionResult ProcessDetailedRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_detailedDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _detailedDao.GetDetailedReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar progress data.",
                    errorDetails = ex.Message
                });
            }
        }

        private IHttpActionResult ProcessSummaryRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_summaryDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _summaryDao.GetSummaryReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar progress data.",
                    errorDetails = ex.Message
                });
            }
        }

        private string ValidateRequestParameters(SolarProgressRequest request)
        {
            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    if (string.IsNullOrEmpty(request.AreaCode))
                        return "Area code is required for Area report type.";
                    break;
                case SolarReportType.Province:
                    if (string.IsNullOrEmpty(request.ProvCode))
                        return "Province code is required for Province report type.";
                    break;
                case SolarReportType.Region:
                    if (string.IsNullOrEmpty(request.Region))
                        return "Region is required for Region report type.";
                    break;
                case SolarReportType.EntireCEB:
                    break;
                default:
                    return "Invalid report type specified.";
            }

            return null;
        }
    }
}


