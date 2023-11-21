using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyScimAPI.Extensions;
using Newtonsoft.Json.Linq;

namespace MyScimAPI.Controllers
{
    [ApiController]
    public class ScimManageController : ControllerBase
    {
        private readonly IScimService _scimService;

        public ScimManageController(IScimService scimService)
        {
            _scimService = scimService;
        }

        [Route("/scim/v2/ServiceProviderConfig")]
        [HttpGet]
        public IActionResult GetServiceProviderConfig()
        {
            var result = _scimService.GetServiceProviderConfig();
            Response.Headers.Add("Etag", (string)result["meta"]["version"]);
            Response.Headers.Add("Content-Type", "application/scim+json");

            return Ok(result);
        }

        [Route("/scim/v2/ResourceTypes")]
        [HttpGet]
        public IActionResult GetResourceTypes()
        {
            var result = _scimService.GetResourceTypes();
            Response.Headers.Add("Content-Type", "application/scim+json");

            return Ok(result);
        }

        [Route("/scim/v2/ResourceTypes/User")]
        [HttpGet]
        public IActionResult GetResourceTypeUser()
        {
            var result = _scimService.GetResourceTypes();
            var resourceTypeUser = result.Where(c => (string)c["name"] == "User").FirstOrDefault();
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Etag", (string)resourceTypeUser["meta"]["version"]);

            return Ok(resourceTypeUser);
        }

        [Route("/scim/v2/ResourceTypes/Group")]
        [HttpGet]
        public IActionResult GetResourceTypeGroup()
        {
            var result = _scimService.GetResourceTypes();
            var resourceTypeGroup = result.Where(c => (string)c["name"] == "Group").FirstOrDefault();
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Etag", (string)resourceTypeGroup["meta"]["version"]);

            return Ok(resourceTypeGroup);
        }

        [Route("/scim/v2/Schemas")]
        [HttpGet]
        public IActionResult GetSchemas()
        {
            var result = _scimService.GetSchemas();
            Response.Headers.Add("Content-Type", "application/scim+json");

            return Ok(result);
        }

        [Route("/scim/v2/Schemas/urn:ietf:params:scim:schemas:core:2.0:User")]
        [HttpGet]
        public IActionResult GetSchemaUser()
        {
            var result = _scimService.GetSchemas();
            var schemaUser = result.Where(c => (string)c["id"] == "urn:ietf:params:scim:schemas:core:2.0:User").FirstOrDefault();
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Etag", (string)schemaUser["meta"]["version"]);

            return Ok(schemaUser);
        }

        [Route("/scim/v2/Schemas/urn:ietf:params:scim:schemas:extension:enterprise:2.0:User")]
        [HttpGet]
        public IActionResult GetSchemaEnterpriseUser()
        {
            var result = _scimService.GetSchemas();
            var schemaEnterpriseUser = result.Where(c => (string)c["id"] == "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User").FirstOrDefault();
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Etag", (string)schemaEnterpriseUser["meta"]["version"]);

            return Ok(schemaEnterpriseUser);
        }

        [Route("/scim/v2/Schemas/urn:ietf:params:scim:schemas:core:2.0:Group")]
        [HttpGet]
        public IActionResult GetSchemaGroup()
        {
            var result = _scimService.GetSchemas();
            var schemaGroup = result.Where(c => (string)c["id"] == "urn:ietf:params:scim:schemas:core:2.0:Group").FirstOrDefault();
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Etag", (string)schemaGroup["meta"]["version"]);

            return Ok(schemaGroup);
        }

        [Route("/scim/v2/Schemas/urn:ietf:params:scim:schemas:core:2.0:Schema")]
        [HttpGet]
        public IActionResult GetSchemaSchema()
        {
            var result = _scimService.GetSchemas();
            var schemaschema = result.Where(c => (string)c["id"] == "urn:ietf:params:scim:schemas:core:2.0:Schema").FirstOrDefault();
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Etag", (string)schemaschema["meta"]["version"]);

            return Ok(schemaschema);
        }

        [Route("/scim/v2/Schemas/urn:ietf:params:scim:schemas:core:2.0:ResourceType")]
        [HttpGet]
        public IActionResult GetSchemaResourceType()
        {
            var result = _scimService.GetSchemas();
            var schemaResourceType = result.Where(c => (string)c["id"] == "urn:ietf:params:scim:schemas:core:2.0:ResourceType").FirstOrDefault();
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Etag", (string)schemaResourceType["meta"]["version"]);

            return Ok(schemaResourceType);
        }

        [Route("/scim/v2/Schemas/urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig")]
        [HttpGet]
        public IActionResult GetSchemaServiceProviderConfig()
        {
            var result = _scimService.GetSchemas();
            var schemaServiceProviderConfig = result.Where(c => (string)c["id"] == "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig").FirstOrDefault();
            Response.Headers.Add("Content-Type", "application/scim+json");
            Response.Headers.Add("Etag", (string)schemaServiceProviderConfig["meta"]["version"]);

            return Ok(schemaServiceProviderConfig);
        }

        [Authorize(Policy = "UsersReadWrite")]
        [Route("/scim/v2/Bulk")]
        [HttpPost]
        public async Task<IActionResult> BulkOperation([FromBody] JObject jObject)
        {
            var result = await _scimService.BulkOperationAsync(jObject);
            Response.Headers.Add("Content-Type", "application/scim+json");
            return Ok(result);
        }

    }
}
