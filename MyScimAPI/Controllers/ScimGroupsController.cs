using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyScimAPI.Extensions;
using Newtonsoft.Json.Linq;
using MyScimAPI.Models;

namespace MyScimAPI.Controllers
{
    [ApiController]

    public class ScimGroupsController : ControllerBase
    {
        private readonly IScimService _scimService;

        public ScimGroupsController(IScimService scimService)
        {
            _scimService = scimService;

        }

        [Authorize(Policy = "GroupsReadWrite")]
        [Route("/scim/v2/Groups")]
        [HttpPost]
        public async Task<IActionResult> AddScimGroup([FromBody] JObject jObject)
        {

            var result = await _scimService.AddScimGroupAsync(jObject);
            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");

            return Created((string)result["meta"]["location"], result);


        }

        [Authorize(Policy = "GroupsRead")]
        [Route("/scim/v2/Groups")]
        [HttpGet]
        public async Task<IActionResult> GetScimGroups([FromQuery] string filter, [FromQuery] string excludedAttributes, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {

            if (!string.IsNullOrEmpty(filter))
            {
                var result = await _scimService.FilterScimGroupsAsync(filter, excludedAttributes);
                var xPagination = $"{{\"TotalCount\": {result["TotalCount"]}, \"TotalPages\": {result["TotalPages"]}, \"CurrentPage\": {result["CurrentPage"]}, \"PageSize\": {result["PageSize"]}}}";
                Response.Headers.Add("Content-Type", "application/scim+json");
                Response.Headers.Add("X-Pagination", xPagination);

                return Ok(result["Items"]);
            }
            else if ((pageNumber != null) && (pageSize != null))
            {

                var result = await _scimService.GetScimGroupsAsync((int)pageNumber, (int)pageSize);
                var xPagination = $"{{\"TotalCount\": {result["TotalCount"]}, \"TotalPages\": {result["TotalPages"]}, \"CurrentPage\": {result["CurrentPage"]}, \"PageSize\": {result["PageSize"]}}}";
                Response.Headers.Add("Content-Type", "application/scim+json");
                Response.Headers.Add("X-Pagination", xPagination);

                return Ok(result["Items"]);
            }
            else
            {
                var pagingParameter = new PagingParameter();
                var result = await _scimService.GetScimGroupsAsync(pagingParameter.CurrentPage, pagingParameter.PageSize);
                var xPagination = $"{{\"TotalCount\": {result["TotalCount"]}, \"TotalPages\": {result["TotalPages"]}, \"CurrentPage\": {result["CurrentPage"]}, \"PageSize\": {result["PageSize"]}}}";
                Response.Headers.Add("Content-Type", "application/scim+json");
                Response.Headers.Add("X-Pagination", xPagination);

                return Ok(result["Items"]);
            }

            
        }

        [Authorize(Policy = "GroupsRead")]
        [Route("/scim/v2/Groups/{id:guid}")]
        [HttpGet]
        public async Task<IActionResult> GetScimGroupById(Guid id, [FromQuery] string excludedAttributes)
        {

            var result = await _scimService.GetScimGroupAsync(id);
            if(!string.IsNullOrEmpty(excludedAttributes) && excludedAttributes == "members")
            {
                result.Remove("members");
            }
            var etag = Request.Headers["Etag"];
            if(!string.IsNullOrEmpty(etag))
            {
                if (_scimService.IsEtagMatchedForResource(etag, result))
                    return StatusCode(304);
            }
            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Location", (string)result["meta"]["location"]);

            return Ok(result);


        }

        [Authorize(Policy = "GroupsReadWrite")]
        [Route("/scim/v2/Groups/{id:guid}")]
        [HttpPut]
        public async Task<IActionResult> PutScimGroupById(Guid id, [FromBody] JObject jObject)
        {

            var result = await _scimService.PutScimGroupAsync(id, jObject);
            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Location", (string)result["meta"]["location"]);

            return Ok(result);
            //return NoContent();

        }

        [Authorize(Policy = "GroupsReadWrite")]
        [Route("/scim/v2/Groups/{id:guid}")]
        [HttpPatch]
        public async Task<IActionResult> PatchScimGroupById(Guid id, [FromBody] JObject jObject)
        {

            var result = await _scimService.PatchScimGroupAsync(id, jObject);
            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Location", (string)result["meta"]["location"]);

            return Ok(result);
            //return NoContent();

        }

        [Authorize(Policy = "GroupsReadWrite")]
        [Route("/scim/v2/Groups/{id:guid}")]
        [HttpDelete]
        public IActionResult RemoveScimGroupById(Guid id)
        {

            _scimService.RemoveScimGroup(id);

            return NoContent();


        }
    }
}
