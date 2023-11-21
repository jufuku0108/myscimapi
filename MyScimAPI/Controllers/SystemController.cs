using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyScimAPI.Extensions;
using MyScimAPI.Data;
using MyScimAPI.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MyScimAPI.Controllers
{
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly ScimDataContext _scimDataContext;
        public SystemController(ScimDataContext scimDataContext)
        {
            _scimDataContext = scimDataContext;
        }

        [Authorize(Policy = "SystemRead")]
        [HttpGet]
        [Route("/system/httplogs")]
        public async Task<IActionResult> GetHttpLogs([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {

            var pagingParameter = new PagingParameter();

            if ((pageNumber != null) && (pageSize != null))
            {
                pagingParameter.CurrentPage = (int)pageNumber;
                pagingParameter.PageSize = (int)pageSize;

            }

            var httpLogs = await _scimDataContext.HttpObjects
                .OrderByDescending(c => c.HttpObjectId)
                .Skip((pagingParameter.CurrentPage - 1) * pagingParameter.PageSize)
                .Take(pagingParameter.PageSize)
                .ToListAsync();

            pagingParameter.TotalCount = _scimDataContext.HttpObjects.Count();
            if ((pagingParameter.TotalCount % pagingParameter.PageSize) == 0)
            {
                pagingParameter.TotalPages = pagingParameter.TotalCount / pagingParameter.PageSize;
            }
            else
            {
                pagingParameter.TotalPages = (pagingParameter.TotalCount / pagingParameter.PageSize) + 1;
            }

            var xPagination = $"{{\"TotalCount\": {pagingParameter.TotalCount}, \"TotalPages\": {pagingParameter.TotalPages}, \"CurrentPage\": {pagingParameter.CurrentPage}, \"PageSize\": {pagingParameter.PageSize}}}";
            Response.Headers.Add("X-Pagination", xPagination);
            Response.Headers.Add("Content-Type", "application/json");

            var result = JsonConvert.SerializeObject(httpLogs,Formatting.Indented);

            var jobject = JArray.Parse(result);
            return Ok(jobject);



        }

        [Authorize(Policy = "SystemRead")]
        [HttpGet]
        [Route("/system/statistics")]
        public async Task<IActionResult> GetStatisticsData()
        {

            var statisticsData = await _scimDataContext.StatisticsData
                .ToListAsync();


            Response.Headers.Add("Content-Type", "application/json");

            var result = JsonConvert.SerializeObject(statisticsData, Formatting.Indented);

            var jobject = JArray.Parse(result);
            return Ok(jobject);



        }
    }
}
