using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MyScimAPI.Extensions;
using Newtonsoft.Json;
using MyScimAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MyScimAPI.Controllers
{

    [ApiController]
    public class ScimUsersController : ControllerBase
    {
        private readonly IScimService _scimService;

        public ScimUsersController(IScimService scimService)
        {
            _scimService = scimService;
        }

        [Authorize(Policy = "UsersReadWrite")]
        [Route("/scim/v2/Users")]
        [HttpPost]
        public async Task<IActionResult> AddScimUser([FromBody] JObject jObject)
        {

            var result = await _scimService.AddScimUserAsync(jObject);
            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");

            return Created((string)result["meta"]["location"], result);

        }

        [Authorize(Policy = "UsersRead")]
        [Route("/scim/v2/Users")]
        [HttpGet]
        public async Task<IActionResult> GetScimUsers([FromQuery] string filter,[FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
           

            if (!string.IsNullOrEmpty(filter))
            {
                var result = await _scimService.FilterScimUsersAsync(filter);
                var xPagination = $"{{\"TotalCount\": {result["TotalCount"]}, \"TotalPages\": {result["TotalPages"]}, \"CurrentPage\": {result["CurrentPage"]}, \"PageSize\": {result["PageSize"]}}}";
                Response.Headers.Add("Content-Type", "application/scim+json");
                Response.Headers.Add("X-Pagination", xPagination);
                return Ok(result["Items"]);
            }
            else if ((pageNumber != null) && (pageSize != null))
            {
                var result = await _scimService.GetScimUsersAsync((int)pageNumber, (int)pageSize);
                var xPagination = $"{{\"TotalCount\": {result["TotalCount"]}, \"TotalPages\": {result["TotalPages"]}, \"CurrentPage\": {result["CurrentPage"]}, \"PageSize\": {result["PageSize"]}}}";
                Response.Headers.Add("Content-Type", "application/scim+json");
                Response.Headers.Add("X-Pagination", xPagination);

                return Ok(result["Items"]);
            }
            else
            {
                var pagingParameter = new PagingParameter();
                var result = await _scimService.GetScimUsersAsync(pagingParameter.CurrentPage, pagingParameter.PageSize);
                var xPagination = $"{{\"TotalCount\": {result["TotalCount"]}, \"TotalPages\": {result["TotalPages"]}, \"CurrentPage\": {result["CurrentPage"]}, \"PageSize\": {result["PageSize"]}}}";
                Response.Headers.Add("Content-Type", "application/scim+json");
                Response.Headers.Add("X-Pagination", xPagination);

                return Ok(result["Items"]);
            }
 


        }

        [Authorize(Policy = "UsersRead")]
        [Route("/scim/v2/Users/{id:guid}")]
        [HttpGet]
        public async Task<IActionResult> GetScimUserById(Guid id)
        {
            if(!ValidateMeScope(id))
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.UnAuthorized };


            var result = await _scimService.GetScimUserAsync(id);
            var etag = Request.Headers["Etag"];
            if (!string.IsNullOrEmpty(etag))
            {
                if (_scimService.IsEtagMatchedForResource(etag, result))
                    return StatusCode(304);
            }

            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Location", (string)result["meta"]["location"]);

            return Ok(result);


        }

        [Authorize(Policy = "UsersReadWrite")]
        [Route("/scim/v2/Users/{id:guid}")]
        [HttpPut]
        public async Task<IActionResult> PutScimUserById(Guid id, [FromBody] JObject jObject)
        {

            var result = await _scimService.PutScimUserAsync(id, jObject);
            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Location", (string)result["meta"]["location"]);

            return Ok(result);
        }

        [Authorize(Policy = "UsersReadWrite")]
        [Route("/scim/v2/Users/{id:guid}")]
        [HttpPatch]
        public async Task<IActionResult> PatchScimUserById(Guid id, [FromBody] JObject jObject)
        {

            var result = await _scimService.PatchScimUserAsync(id, jObject);
            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Location", (string)result["meta"]["location"]);

            return Ok(result);

        }

        [Authorize(Policy = "UsersReadWrite")]
        [Route("/scim/v2/Users/{id:guid}")]
        [HttpDelete]
        public IActionResult RemoveScimUserById(Guid id)
        {

            _scimService.RemoveScimUser(id);

            return NoContent();


        }


        [Authorize(Policy = "Me")]
        [Route("/scim/v2/me")]
        [HttpGet]
        public async Task<IActionResult> GetScimMe()
        {
            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var sub = claimsIdentity.Claims.Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").FirstOrDefault();

            var result = await _scimService.GetScimUserAsync(Guid.Parse(sub.Value.ToString()));

            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Location", (string)result["meta"]["location"]);

            return Ok(result);

        }

        private bool ValidateMeScope(Guid id)
        {
            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var claims = claimsIdentity.Claims.ToList();
            var me = claims.AsQueryable().Where(c => c.Value == "me").FirstOrDefault();
            if (me != null)
            {
                var usersRead = claims.AsQueryable().Where(c => c.Value == "users.read").FirstOrDefault();
                var usersReadWrite = claims.AsQueryable().Where(c => c.Value == "users.read.write").FirstOrDefault();

                if (usersRead == null && usersReadWrite == null)
                {
                    var sub = claims.AsQueryable().Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").FirstOrDefault();
                    if (id.ToString() != sub.Value.ToString())
                        return false;

                }
            }
            return true;

        }

    }
}
