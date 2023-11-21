using System;
using System.Collections.Generic;
using System.Text;
using MyScimAPI.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MyScimAPI.Extensions
{
    public interface IScimService
    {
        JObject GetServiceProviderConfig();
        JArray GetResourceTypes();
        JArray GetSchemas();
        Task<JObject> BulkOperationAsync(JObject jObject);
        bool IsEtagMatchedForResource(string etag, JObject jObject);
        JObject CreateJsonException(ScimErrorException scimErrorException);

        Task<JObject> AddScimUserAsync(JObject jObject);
        Task<JObject> GetScimUserAsync(Guid id);
        Task<JObject> GetScimUsersAsync(int pageNumber, int pageSize);
        Task<JObject> FilterScimUsersAsync(string filter);
        Task<JObject> PutScimUserAsync(Guid id, JObject jObject);
        Task<JObject> PatchScimUserAsync(Guid id, JObject jObject);
        void RemoveScimUser(Guid id);

        Task<JObject> AddScimGroupAsync(JObject jObject);
        Task<JObject> GetScimGroupAsync(Guid id);
        Task<JObject> GetScimGroupsAsync(int pageNumber, int pageSize);
        Task<JObject> FilterScimGroupsAsync(string filter, string excludedAttributes);
        Task<JObject> PutScimGroupAsync(Guid id, JObject jObject);
        Task<JObject> PatchScimGroupAsync(Guid id, JObject jObject);
        void RemoveScimGroup(Guid id);



    }
}
