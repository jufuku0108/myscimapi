using System;
using System.Collections.Generic;
using System.Text;
using MyScimAPI.Data;
using System.Threading.Tasks;
using MyScimAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace MyScimAPI.Extensions
{
    public class ScimService : IScimService
    {
        private readonly ScimDataContext _scimDataContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ScimService(ScimDataContext scimDataContext, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment)
        {
            _scimDataContext = scimDataContext;
            _httpContextAccessor = httpContextAccessor;
            _webHostEnvironment = webHostEnvironment;
        }
        public JObject GetServiceProviderConfig()
        {
            var jObject = new JObject();
            var configPath = _webHostEnvironment.ContentRootPath + "\\Extensions\\ServiceProviderConfig.json";
            
            using (StreamReader sr = new StreamReader(configPath))
            {
                jObject = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
            }

            jObject["meta"]["location"] = "https://" + _httpContextAccessor.HttpContext.Request.Host.Value + "/scim/v2/ServiceProviderConfig";

            var version = Convert.ToBase64String(Encoding.UTF8.GetBytes((string)jObject["meta"]["lastModified"]));
            var etag = "W\\/\"" + version + "\"";
            jObject["meta"]["version"] = etag;


            return jObject;
        }
        public JArray GetResourceTypes()
        {
            var jArray = new JArray();
            var configPath = _webHostEnvironment.ContentRootPath + "\\Extensions\\ResourceType.json";

            using (StreamReader sr = new StreamReader(configPath))
            {
                jArray = (JArray)JsonConvert.DeserializeObject(sr.ReadToEnd());
            }

            foreach (JObject jObject in jArray)
            {
                jObject["meta"]["location"] = "https://" + _httpContextAccessor.HttpContext.Request.Host.Value + "/scim/v2/ResourceTypes/" + jObject["name"];
                var version = Convert.ToBase64String(Encoding.UTF8.GetBytes((string)jObject["meta"]["lastModified"]));
                var etag = "W\\/\"" + version + "\"";
                jObject["meta"]["version"] = etag;

            }

            return jArray;
        }
        public JArray GetSchemas()
        {
            var jArray = new JArray();
            var configPath = _webHostEnvironment.ContentRootPath + "\\Extensions\\Schemas.json";

            using (StreamReader sr = new StreamReader(configPath))
            {
                jArray = (JArray)JsonConvert.DeserializeObject(sr.ReadToEnd());
            }

            foreach(JObject jObject in jArray)
            {
                jObject["meta"]["location"] = "https://" + _httpContextAccessor.HttpContext.Request.Host.Value + "/scim/v2/Schems/" + jObject["id"];
                var version = Convert.ToBase64String(Encoding.UTF8.GetBytes((string)jObject["meta"]["lastModified"]));
                var etag = "W\\/\"" + version + "\"";
                jObject["meta"]["version"] = etag;

            }

            return jArray;
        }

        public async Task<JObject> BulkOperationAsync(JObject jObject)
        {
            if((string)jObject["schemas"][0] != "urn:ietf:params:scim:api:messages:2.0:BulkRequest")
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };
            if((JArray)jObject["Operations"] == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };
            
            var operationsResults = new JArray();

            Dictionary<string,string> bulkIdsTobeMembers = new Dictionary<string, string>();
            Dictionary<string,string> bulkIdsTobeManagers = new Dictionary<string, string>();

            var operations = (JArray)jObject["Operations"];
            foreach(var operation in operations)
            {
                var bulkId = (string)operation["bulkId"];
                
                var path = (string)operation["path"];

                var data = (JObject)operation["data"];

                var method = (string)operation["method"];

                try
                {

                    switch (method)
                    {
                        case "POST":
                            switch (path)
                            {
                                case "/Users":
                                    var bulkCreatedUser = await AddScimUserAsync(data);
                                    operationsResults.Add(CreateBulkResourceJobject(method, bulkId, "201", bulkCreatedUser));
                                    if (data["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"] != null)
                                    {
                                        if(data["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"] != null)
                                        {
                                            var managerBulkId = data["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["value"].ToString().Replace("bulkId:", "");
                                            bulkIdsTobeManagers.Add(bulkId, managerBulkId);
                                        }
                                    }

                                    break;
                                case "/Groups":
                                    var bulkCreatedGroup = await AddScimGroupAsync(data);
                                    operationsResults.Add(CreateBulkResourceJobject(method, bulkId, "201", bulkCreatedGroup));
                                    if(data["members"] != null)
                                    {
                                        foreach(var member in data["members"])
                                        {
                                            var value = (string)member["value"];
                                            if (value.Contains("bulkId:"))
                                            {
                                                var memberBulkId = value.Replace("bulkId:", "");
                                                bulkIdsTobeMembers.Add(bulkId, memberBulkId);
                                            }
                                        }
                                    }
                                    break;
                            }

                            break;
                        case "DELETE":
                            if (path.Contains("/Users"))
                            {
                                var userId = path.Substring(path.ToString().Length - 36);
                                var scimUserJobject = await GetScimUserAsync(Guid.Parse(userId));
                                RemoveScimUser(Guid.Parse(userId));
                                operationsResults.Add(CreateBulkResourceJobject(method, bulkId, "204", scimUserJobject));

                            }
                            else if (path.Contains("/Groups"))
                            {
                                var groupId = path.Substring(path.ToString().Length - 36);
                                var scimGroupJobject = await GetScimGroupAsync(Guid.Parse(groupId));
                                RemoveScimGroup(Guid.Parse(groupId));
                                operationsResults.Add(CreateBulkResourceJobject(method, bulkId, "204", scimGroupJobject));
                            }
                            break;
                    }

                }
                catch
                {
                    var erroResponse = CreateJsonException(new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" });
                    operationsResults.Add(CreateBulkErrorResourceJobject(method, bulkId, "400", erroResponse));
                }
                
            
            }
            
            foreach(var bulkIdsTobeMember in bulkIdsTobeMembers)
            {
                var keyGroup = operationsResults.Where(c => (string)c["bulkId"] == bulkIdsTobeMember.Key).FirstOrDefault();
                var keyGroupId = (string)keyGroup["location"].ToString().Substring(keyGroup["location"].ToString().Length - 36);

                var keyScimGroup = _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == Guid.Parse(keyGroupId))
                    .Include(c => c.Members)
                    .Include(c => c.Meta)
                    .FirstOrDefault();


                var memberResource = operationsResults.Where(c => (string)c["bulkId"] == bulkIdsTobeMember.Value).FirstOrDefault();
                var memberResourceId = (string)memberResource["location"].ToString().Substring(memberResource["location"].ToString().Length - 36);

                
                if (memberResource["location"].ToString().Contains("/Users"))
                {
                    var memberScimUser = _scimDataContext.ScimUsers.Where(c => c.ScimUserId == Guid.Parse(memberResourceId))
                        .Include(c => c.Name)
                        .Include(c => c.Emails)
                        .Include(c => c.Addresses)
                        .Include(c => c.PhoneNumbers)
                        .Include(c => c.Ims)
                        .Include(c => c.Photos)
                        .Include(c => c.Groups)
                        .Include(c => c.Entitlements)
                        .Include(c => c.Roles)
                        .Include(c => c.X509Certificates)
                        .Include(c => c.EnterpriseUser)
                        .Include(c => c.EnterpriseUser.Manager)
                        .Include(c => c.Meta)
                        .FirstOrDefault();

                    
                    var keyScimGroupMember = keyScimGroup.Members.Where(c => c.Value.Contains(bulkIdsTobeMember.Value)).FirstOrDefault();
                    keyScimGroupMember.Value = memberScimUser.ScimUserId.ToString();
                    keyScimGroupMember.Display = memberScimUser.DisplayName;
                    keyScimGroupMember.Ref = memberScimUser.Meta.Location;
                    _scimDataContext.ScimGroupMembers.Update(keyScimGroupMember);
                    _scimDataContext.SaveChanges();
                    

                }
                else if (memberResource["location"].ToString().Contains("/Groups"))
                {
                    var memberScimGroup = _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == Guid.Parse(memberResourceId))
                        .Include(c => c.Members)
                        .Include(c => c.Meta)
                        .FirstOrDefault();

                    var keyScimGroupMember = keyScimGroup.Members.Where(c => c.Value.Contains(bulkIdsTobeMember.Value)).FirstOrDefault();
                    keyScimGroupMember.Value = memberScimGroup.ScimGroupId.ToString();
                    keyScimGroupMember.Display = memberScimGroup.DisplayName;
                    keyScimGroupMember.Ref = memberScimGroup.Meta.Location;
                    _scimDataContext.ScimGroupMembers.Update(keyScimGroupMember);
                    _scimDataContext.SaveChanges();
                }

            }

            foreach (var bulkIdsTobeManager in bulkIdsTobeManagers)
            {
                var keyUser = operationsResults.Where(c => (string)c["bulkId"] == bulkIdsTobeManager.Key).FirstOrDefault();
                var keyUserId = (string)keyUser["location"].ToString().Substring(keyUser["location"].ToString().Length - 36);

                var keyScimUser = _scimDataContext.ScimUsers.Where(c => c.ScimUserId == Guid.Parse(keyUserId))
                        .Include(c => c.Name)
                        .Include(c => c.Emails)
                        .Include(c => c.Addresses)
                        .Include(c => c.PhoneNumbers)
                        .Include(c => c.Ims)
                        .Include(c => c.Photos)
                        .Include(c => c.Groups)
                        .Include(c => c.Entitlements)
                        .Include(c => c.Roles)
                        .Include(c => c.X509Certificates)
                        .Include(c => c.EnterpriseUser)
                        .Include(c => c.EnterpriseUser.Manager)
                        .Include(c => c.Meta)
                        .FirstOrDefault();

                var managerUser = operationsResults.Where(c => (string)c["bulkId"] == bulkIdsTobeManager.Value).FirstOrDefault();
                var managerUserId = (string)managerUser["location"].ToString().Substring(managerUser["location"].ToString().Length - 36);

                var managerScimUser = _scimDataContext.ScimUsers.Where(c => c.ScimUserId == Guid.Parse(managerUserId))
                    .Include(c => c.Name)
                    .Include(c => c.Emails)
                    .Include(c => c.Addresses)
                    .Include(c => c.PhoneNumbers)
                    .Include(c => c.Ims)
                    .Include(c => c.Photos)
                    .Include(c => c.Groups)
                    .Include(c => c.Entitlements)
                    .Include(c => c.Roles)
                    .Include(c => c.X509Certificates)
                    .Include(c => c.EnterpriseUser)
                    .Include(c => c.EnterpriseUser.Manager)
                    .Include(c => c.Meta)
                    .FirstOrDefault();

                keyScimUser.EnterpriseUser.Manager.Value = managerScimUser.ScimUserId.ToString();
                keyScimUser.EnterpriseUser.Manager.Ref = managerScimUser.Meta.Location;
                keyScimUser.EnterpriseUser.Manager.DisplayName = managerScimUser.DisplayName;

                _scimDataContext.ScimUserManagers.Update(keyScimUser.EnterpriseUser.Manager);
                _scimDataContext.SaveChanges();

            }

            var result = CreateBulkResponseJobject(operationsResults);
            return result;

        }

        public bool IsEtagMatchedForResource(string etag, JObject jObject)
        {

            var convertedEtag = etag.Split("\"")[1];

            var version = (string)jObject["meta"]["version"];
            var convertedVersion = version.Split("\"")[1];

            if (String.Equals(convertedVersion, convertedEtag))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task<JObject> AddScimUserAsync(JObject jObject)
        {
            var scimUser = new ScimUser();

            if (jObject["id"] != null)
            {
                scimUser.ScimUserId =  Guid.Parse((string)jObject["id"]);
            }
            else
            {
                scimUser.ScimUserId = Guid.NewGuid();
            }
            

            try
            {


                //await _scimDataContext.ApplicationUsers.AddAsync(applicationUser);
                await _scimDataContext.ScimUsers.AddAsync(scimUser);
                await _scimDataContext.SaveChangesAsync();

                AddOrUpdateAttributesForScimUserByJson(scimUser, jObject);
                AddOrUpdateAttributeForScimUserByPath(scimUser, jObject, "meta");

                var createdScimUser = await _scimDataContext.ScimUsers.Where(c => c.ScimUserId == scimUser.ScimUserId)
                    .Include(c => c.Name)
                    .Include(c => c.Emails)
                    .Include(c => c.Addresses)
                    .Include(c => c.PhoneNumbers)
                    .Include(c => c.Ims)
                    .Include(c => c.Photos)
                    .Include(c => c.Groups)
                    .Include(c => c.Entitlements)
                    .Include(c => c.Roles)
                    .Include(c => c.X509Certificates)
                    .Include(c => c.EnterpriseUser)
                    .Include(c => c.EnterpriseUser.Manager)
                    .Include(c => c.Meta)
                    .FirstOrDefaultAsync();

                //applicationUser.UserName = createdScimUser.UserName;
                //applicationUser.EmailConfirmed = true;


                //_scimDataContext.ApplicationUsers.Update(applicationUser);
                //_scimDataContext.SaveChanges();

                var result = CreateReturnScimUserJObject(createdScimUser);
         
                return result;

            } 
            catch 
            {
                RemoveScimUser(scimUser.ScimUserId);
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };

            }
        }
        public async Task<JObject> GetScimUserAsync(Guid id)
        {
            var scimUser = await _scimDataContext.ScimUsers.Where(c => c.ScimUserId == id)
                .Include(c => c.Name)
                .Include(c => c.Emails)
                .Include(c => c.Addresses)
                .Include(c => c.PhoneNumbers)
                .Include(c => c.Ims)
                .Include(c => c.Photos)
                .Include(c => c.Groups)
                .Include(c => c.Entitlements)
                .Include(c => c.Roles)
                .Include(c => c.X509Certificates)
                .Include(c => c.EnterpriseUser)
                .Include(c => c.EnterpriseUser.Manager)
                .Include(c => c.Meta)
                .FirstOrDefaultAsync();
            if(scimUser == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.NotFound };

            var result = CreateReturnScimUserJObject(scimUser);
            return result;

        }
        public async Task<JObject> GetScimUsersAsync(int pageNumber, int pageSize)
        {
            var pagingParameter = new PagingParameter { CurrentPage = pageNumber, PageSize = pageSize };
            var scimUsers = await _scimDataContext.ScimUsers
                .Skip((pagingParameter.CurrentPage - 1) * pagingParameter.PageSize)
                .Take(pagingParameter.PageSize)
                .Include(c => c.Name)
                .Include(c => c.Emails)
                .Include(c => c.Addresses)
                .Include(c => c.PhoneNumbers)
                .Include(c => c.Ims)
                .Include(c => c.Photos)
                .Include(c => c.Groups)
                .Include(c => c.Entitlements)
                .Include(c => c.Roles)
                .Include(c => c.X509Certificates)
                .Include(c => c.EnterpriseUser)
                .Include(c => c.EnterpriseUser.Manager)
                .Include(c => c.Meta)
                .ToListAsync();

            pagingParameter.TotalCount = _scimDataContext.ScimUsers.Count();
            if((pagingParameter.TotalCount % pagingParameter.PageSize) == 0)
            {
                pagingParameter.TotalPages = pagingParameter.TotalCount / pagingParameter.PageSize;
            }
            else
            {
                pagingParameter.TotalPages = (pagingParameter.TotalCount / pagingParameter.PageSize) + 1;
            }

            JArray scimUsersJArray = new JArray();

            foreach (var scimUser in scimUsers)
            {
                scimUsersJArray.Add(CreateReturnScimUserJObject(scimUser));
            }
            var result = CreateReturnPagingObject(pagingParameter, scimUsersJArray);
            return result;
        }
        public async Task<JObject> FilterScimUsersAsync(string filter)
        {
            var pagingParameter = new PagingParameter();

            var scimUsers = await FilterScimUsersCoreAsync(filter);
            pagingParameter.TotalCount = scimUsers.Count();
            
            if ((pagingParameter.TotalCount % pagingParameter.PageSize) == 0)
            {
                pagingParameter.TotalPages = pagingParameter.TotalCount / pagingParameter.PageSize;
            }
            else
            {
                pagingParameter.TotalPages = (pagingParameter.TotalCount / pagingParameter.PageSize) + 1;
            }

            JArray scimUsersJArray = new JArray();

            foreach(var scimUser in scimUsers)
            {
                scimUsersJArray.Add(CreateReturnScimUserJObject(scimUser));
            }
            //var result = CreateReturnScimResourceJobjectList(scimUsersJArray);
            var result = CreateReturnPagingObject(pagingParameter, scimUsersJArray);

            return result;
        }
        public async Task<JObject> PutScimUserAsync(Guid id, JObject jObject)
        {
            var putScimUser = await PutScimUserCoreAsync(id, jObject);
            var result = CreateReturnScimUserJObject(putScimUser);
            return result;
        }
        public async Task<JObject> PatchScimUserAsync(Guid id,JObject jObject)
        {

            var patchedScimUser = await PatchScimUserCoreAync(id, jObject);
            var result = CreateReturnScimUserJObject(patchedScimUser);
            return result;
        }
        public void RemoveScimUser(Guid id)
        {
            
            var scimUser = _scimDataContext.ScimUsers.Where(c => c.ScimUserId == id)
                .Include(c => c.Name)
                .Include(c => c.Emails)
                .Include(c => c.Addresses)
                .Include(c => c.PhoneNumbers)
                .Include(c => c.Ims)
                .Include(c => c.Photos)
                .Include(c => c.Groups)
                .Include(c => c.Entitlements)
                .Include(c => c.Roles)
                .Include(c => c.X509Certificates)
                .Include(c => c.EnterpriseUser)
                .Include(c => c.EnterpriseUser.Manager)
                .Include(c => c.Meta)
                .FirstOrDefault();
            //var applicationUser = _scimDataContext.ApplicationUsers.Where(c => c.ScimUser.ScimUserId == scimUser.ScimUserId).FirstOrDefault();

            _scimDataContext.ScimUsers.Remove(scimUser);
            _scimDataContext.SaveChanges();
        }


        public async Task<JObject> AddScimGroupAsync(JObject jObject)
        {
            var scimGroup = new ScimGroup { ScimGroupId = Guid.NewGuid() };

            try
            {
                await _scimDataContext.ScimGroups.AddAsync(scimGroup);
                await _scimDataContext.SaveChangesAsync();

                
                AddOrUpdateAttributesForScimGroupByJson(scimGroup, jObject);
                AddOrUpdateAttributeForScimGroupByPath(scimGroup, jObject, "meta");
                

                var createdScimGroup = await _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == scimGroup.ScimGroupId)
                    .Include(c => c.Members)
                    .Include(c => c.Meta)
                    .FirstOrDefaultAsync();


                var result = CreateReturnScimGroupJObject(createdScimGroup);

                return result;
            }
            catch
            {
                RemoveScimGroup(scimGroup.ScimGroupId);
                throw new ScimErrorException() {ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };
            }
        }
        public async Task<JObject> GetScimGroupAsync(Guid id)
        {
             var scimGroup = await _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == id)
                .Include(c => c.Members)
                .Include(c => c.Meta)
                .FirstOrDefaultAsync();
            if(scimGroup == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.NotFound };

            var result = CreateReturnScimGroupJObject(scimGroup);
            return result;

        }
        public async Task<JObject> GetScimGroupsAsync(int pageNumber, int pageSize)
        {
            var pagingParameter = new PagingParameter { CurrentPage = pageNumber, PageSize = pageSize };
            var scimGroups = await _scimDataContext.ScimGroups
                .Skip((pagingParameter.CurrentPage - 1) * pagingParameter.PageSize)
                .Take(pagingParameter.PageSize)
                .Include(c => c.Members)
                .Include(c => c.Meta)
                .ToListAsync();

            pagingParameter.TotalCount = _scimDataContext.ScimGroups.Count();
            if ((pagingParameter.TotalCount % pagingParameter.PageSize) == 0)
            {
                pagingParameter.TotalPages = pagingParameter.TotalCount / pagingParameter.PageSize;
            }
            else
            {
                pagingParameter.TotalPages = (pagingParameter.TotalCount / pagingParameter.PageSize) + 1;
            }

            JArray scimGroupsJArray = new JArray();

            foreach (var scimGroup in scimGroups)
            {
                scimGroupsJArray.Add(CreateReturnScimGroupJObject(scimGroup));
            }
            var result = CreateReturnPagingObject(pagingParameter, scimGroupsJArray);
            return result;
        }
        public async Task<JObject> FilterScimGroupsAsync(string filter, string excludedAttributes = null)
        {
            var pagingParameter = new PagingParameter();
            var scimGroups = await FilterScimGroupsCoreAsync(filter, excludedAttributes);
            pagingParameter.TotalCount = scimGroups.Count();

            if ((pagingParameter.TotalCount % pagingParameter.PageSize) == 0)
            {
                pagingParameter.TotalPages = pagingParameter.TotalCount / pagingParameter.PageSize;
            }
            else
            {
                pagingParameter.TotalPages = (pagingParameter.TotalCount / pagingParameter.PageSize) + 1;
            }

            JArray scimGroupsJArray = new JArray();

            foreach (var scimGroup in scimGroups)
            {
                scimGroupsJArray.Add(CreateReturnScimGroupJObject(scimGroup));
            }
            //var result = CreateReturnScimResourceJobjectList(scimGroupsJArray);
            var result = CreateReturnPagingObject(pagingParameter, scimGroupsJArray);
            return result;
        }
        public async Task<JObject> PutScimGroupAsync(Guid id, JObject jObject)
        {
            var putScimGroup = await PutScimGroupCoreAsync(id, jObject);
            var result = CreateReturnScimGroupJObject(putScimGroup);
            return result;
        }
        public async Task<JObject> PatchScimGroupAsync(Guid id, JObject jObject)
        {

            var patchedScimGroup = await PatchScimGroupCoreAync(id, jObject);
            var result = CreateReturnScimGroupJObject(patchedScimGroup);
            return result;
        }
        public void RemoveScimGroup(Guid id)
        {
            var scimGroup = _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == id)
                .Include(c => c.Members)
                .Include(c => c.Meta)
                .FirstOrDefault();
            _scimDataContext.ScimGroups.Remove(scimGroup);
            _scimDataContext.SaveChanges();
        }



        public JObject CreateJsonException(ScimErrorException scimErrorException)
        {
            var result = new JObject();
            if (scimErrorException.Schemas.Count() > 0)
                result.Add(new JProperty("schemas", scimErrorException.Schemas));
            if (scimErrorException.ScimType != null)
                result.Add(new JProperty("scimType", scimErrorException.ScimType));
            if (scimErrorException.Detail != null)
                result.Add(new JProperty("detail", scimErrorException.Detail));
            if (scimErrorException.Status != null)
                result.Add(new JProperty("status", scimErrorException.Status));
            return result;
        }

        #region

        private JObject CreateBulkResourceJobject(string method, string bulkId, string status, JObject jObject)
        {
            var result = new JObject();
            if ((string)jObject["meta"]["location"] != null)
                result.Add(new JProperty("location", (string)jObject["meta"]["location"]));
            
            result.Add(new JProperty("method", method));
    
            if(bulkId != null)
                result.Add(new JProperty("bulkId", bulkId));

            if ((string)jObject["meta"]["version"] != null)
                result.Add(new JProperty("version", (string)jObject["meta"]["version"]));

            result.Add(new JProperty("status", status));

            return result;

        }
        private JObject CreateBulkErrorResourceJobject(string method, string bulkId, string status, JObject jObject)
        {
            var result = new JObject() { };
            result.Add(new JProperty("method", method));
    
            if(bulkId != null)
                result.Add(new JProperty("bulkId", bulkId));
            
            result.Add(new JProperty("status", status));
            result.Add(new JProperty("response", jObject));
            return result;

        }
        private JObject CreateBulkResponseJobject(JArray jArray)
        {
            var result = new JObject() { };

            result.Add(new JProperty("schemas", new JArray("urn: ietf:params:scim:api:messages:2.0:BulkResponse")));
            result.Add(new JProperty("Operations", jArray));
            return result;

        }

        private JObject CreateReturnPagingObject(PagingParameter pagingParameter, JArray jArray)
        {
            var items = CreateReturnScimResourceJobjectList(jArray);
            return new JObject
            {
                new JProperty("TotalCount", pagingParameter.TotalCount),
                new JProperty("TotalPages", pagingParameter.TotalPages),
                new JProperty("CurrentPage", pagingParameter.CurrentPage),
                new JProperty("PageSize", pagingParameter.PageSize),
                new JProperty("Items",items)
            };
        }
        private JObject CreateReturnScimResourceJobjectList(JArray jArray)
        {
            var result = new JObject { };
            result.Add(new JProperty("schemas", new JArray() { "urn:ietf:params:scim:api:messages:2.0:ListResponse" }));
            result.Add(new JProperty("totalResults", jArray.Count));
            result.Add(new JProperty("itemsPerPage", jArray.Count));
            result.Add(new JProperty("startIndex", 1));
            result.Add(new JProperty("Resources", jArray));

            return result;
        }
        private JObject CreateReturnScimUserJObject(ScimUser scimUser)
        {
            var jObject = new JObject();

            if (scimUser.Schemas != null)
            {
                var jSchema = new JArray();
                var schemas = scimUser.Schemas.Split(",");
                foreach(var schema in schemas)
                {
                    jSchema.Add(schema);
                }

                jObject.Add(new JProperty("schemas", jSchema));

            }

            if (scimUser.ScimUserId != null)
                jObject.Add(new JProperty("id", scimUser.ScimUserId));
            
            if (scimUser.ExternalId != null)
                jObject.Add(new JProperty("externalId", scimUser.ExternalId));

            if (scimUser.UserName != null)
                jObject.Add(new JProperty("userName", scimUser.UserName));

            if(scimUser.Name != null)
            {
                var jOName = new JObject();
                if (scimUser.Name.Formatted != null)
                    jOName.Add(new JProperty("formatted", scimUser.Name.Formatted));

                if (scimUser.Name.FamilyName != null)
                    jOName.Add(new JProperty("familyName", scimUser.Name.FamilyName));

                if (scimUser.Name.GivenName != null)
                    jOName.Add(new JProperty("givenName", scimUser.Name.GivenName));

                if (scimUser.Name.MiddleName != null)
                    jOName.Add(new JProperty("middleName", scimUser.Name.MiddleName));

                if (scimUser.Name.HonorificPrefix != null)
                    jOName.Add(new JProperty("honorificPrefix", scimUser.Name.HonorificPrefix));

                if (scimUser.Name.HonorificSuffix != null)
                    jOName.Add(new JProperty("honorificSuffix", scimUser.Name.HonorificSuffix));

                jObject.Add(new JProperty("name", jOName));
            }

            if (scimUser.DisplayName != null)
                jObject.Add(new JProperty("displayName", scimUser.DisplayName));

            if (scimUser.NickName != null)
                jObject.Add(new JProperty("nickName", scimUser.NickName));

            if (scimUser.ProfileUrl != null)
                jObject.Add(new JProperty("profileUrl", scimUser.ProfileUrl));

            if (scimUser.Title != null)
                jObject.Add(new JProperty("title", scimUser.Title));

            if (scimUser.UserType != null)
                jObject.Add(new JProperty("userType", scimUser.UserType));

            if (scimUser.PreferredLanguage != null)
                jObject.Add(new JProperty("preferredLanguage", scimUser.PreferredLanguage));

            if (scimUser.Locale != null)
                jObject.Add(new JProperty("locale", scimUser.Locale));

            if (scimUser.TimeZone != null)
                jObject.Add(new JProperty("timezone", scimUser.TimeZone));

            jObject.Add(new JProperty("active", scimUser.Active));

            if (scimUser.Password != null)
                jObject.Add(new JProperty("password", scimUser.Password));

            if (scimUser.Emails.Count() > 0)
            {
                var jOEmals = new JArray();
                foreach(var scimUserEmail in scimUser.Emails)
                {
                    var JOEmail = new JObject();

                    if (scimUserEmail.Value != null)
                        JOEmail.Add(new JProperty("value", scimUserEmail.Value));

                    if (scimUserEmail.Display != null)
                        JOEmail.Add(new JProperty("display", scimUserEmail.Display));

                    if (scimUserEmail.Type != null)
                        JOEmail.Add(new JProperty("type", scimUserEmail.Type));

                    JOEmail.Add(new JProperty("primary", scimUserEmail.Primary));

                    jOEmals.Add(JOEmail);

                }
                jObject.Add(new JProperty("emails", jOEmals));
            }


            if (scimUser.PhoneNumbers.Count() > 0)
            {
                var jOPhoneNumbers = new JArray();
                foreach (var scimUserPhoneNumber in scimUser.PhoneNumbers)
                {
                    var JOAddress = new JObject();

                    if (scimUserPhoneNumber.Value != null)
                        JOAddress.Add(new JProperty("value", scimUserPhoneNumber.Value));

                    if (scimUserPhoneNumber.Display != null)
                        JOAddress.Add(new JProperty("display", scimUserPhoneNumber.Display));

                    if (scimUserPhoneNumber.Type != null)
                        JOAddress.Add(new JProperty("type", scimUserPhoneNumber.Type));

                    JOAddress.Add(new JProperty("primary", scimUserPhoneNumber.Primary));

                    jOPhoneNumbers.Add(JOAddress);

                }
                jObject.Add(new JProperty("phoneNumbers", jOPhoneNumbers));
            }

            if (scimUser.Ims.Count() > 0)
            {
                var jOIms = new JArray();
                foreach (var scimUserIm in scimUser.Ims)
                {
                    var JOIm = new JObject();

                    if (scimUserIm.Value != null)
                        JOIm.Add(new JProperty("value", scimUserIm.Value));

                    if (scimUserIm.Display != null)
                        JOIm.Add(new JProperty("display", scimUserIm.Display));

                    if (scimUserIm.Type != null)
                        JOIm.Add(new JProperty("type", scimUserIm.Type));

                    JOIm.Add(new JProperty("primary", scimUserIm.Primary));

                    jOIms.Add(JOIm);

                }
                jObject.Add(new JProperty("ims", jOIms));
            }

            if (scimUser.Photos.Count() > 0)
            {
                var jOPhotos = new JArray();
                foreach (var scimUserPhoto in scimUser.Photos)
                {
                    var JOPhoto = new JObject();

                    if (scimUserPhoto.Value != null)
                        JOPhoto.Add(new JProperty("value", scimUserPhoto.Value));

                    if (scimUserPhoto.Display != null)
                        JOPhoto.Add(new JProperty("display", scimUserPhoto.Display));

                    if (scimUserPhoto.Type != null)
                        JOPhoto.Add(new JProperty("type", scimUserPhoto.Type));

                    JOPhoto.Add(new JProperty("primary", scimUserPhoto.Primary));

                    jOPhotos.Add(JOPhoto);

                }
                jObject.Add(new JProperty("photos", jOPhotos));
            }

            if (scimUser.Addresses.Count() > 0)
            {
                var jOAddresses = new JArray();
                foreach (var scimUserAddress in scimUser.Addresses)
                {
                    var JOAddress = new JObject();

                    if (scimUserAddress.Formatted != null)
                        JOAddress.Add(new JProperty("formatted", scimUserAddress.Formatted));

                    if (scimUserAddress.StreetAddress != null)
                        JOAddress.Add(new JProperty("streetAddress", scimUserAddress.StreetAddress));

                    if (scimUserAddress.Locality != null)
                        JOAddress.Add(new JProperty("locality", scimUserAddress.Locality));

                    if (scimUserAddress.Region != null)
                        JOAddress.Add(new JProperty("region", scimUserAddress.Region));

                    if (scimUserAddress.PostalCode != null)
                        JOAddress.Add(new JProperty("postalCode", scimUserAddress.PostalCode));

                    if (scimUserAddress.Country != null)
                        JOAddress.Add(new JProperty("country", scimUserAddress.Country));

                    if (scimUserAddress.Type != null)
                        JOAddress.Add(new JProperty("type", scimUserAddress.Type));

                    jOAddresses.Add(JOAddress);

                }
                jObject.Add(new JProperty("addresses", jOAddresses));
            }


            if (scimUser.Groups.Count() > 0)
            {
                var jOGroups = new JArray();
                foreach (var scimUserGroup in scimUser.Groups)
                {
                    var JOGroup = new JObject();

                    if (scimUserGroup.Value != null)
                        JOGroup.Add(new JProperty("value", scimUserGroup.Value));

                    if (scimUserGroup.Ref != null)
                        JOGroup.Add(new JProperty("$ref", scimUserGroup.Ref));

                    if (scimUserGroup.Display != null)
                        JOGroup.Add(new JProperty("display", scimUserGroup.Display));

                    jOGroups.Add(JOGroup);

                }
                jObject.Add(new JProperty("groups", jOGroups));
            }

            if (scimUser.Entitlements.Count() > 0)
            {
                var jOEntitlements = new JArray();
                foreach (var scimUserEntitlement in scimUser.Entitlements)
                {
                    var jOEntitlement = new JObject();

                    if (scimUserEntitlement.Value != null)
                        jOEntitlement.Add(new JProperty("value", scimUserEntitlement.Value));

                    if (scimUserEntitlement.Display != null)
                        jOEntitlement.Add(new JProperty("display", scimUserEntitlement.Display));

                    if (scimUserEntitlement.Type != null)
                        jOEntitlement.Add(new JProperty("type", scimUserEntitlement.Type));

                    jOEntitlement.Add(new JProperty("primary", scimUserEntitlement.Primary));

                    jOEntitlements.Add(jOEntitlement);

                }
                jObject.Add(new JProperty("entitlements", jOEntitlements));
            }

            if (scimUser.Roles.Count() > 0)
            {
                var jORoles = new JArray();
                foreach (var scimUserRole in scimUser.Roles)
                {
                    var jORole = new JObject();

                    if (scimUserRole.Value != null)
                        jORole.Add(new JProperty("value", scimUserRole.Value));

                    if (scimUserRole.Display != null)
                        jORole.Add(new JProperty("display", scimUserRole.Display));

                    if (scimUserRole.Type != null)
                        jORole.Add(new JProperty("type", scimUserRole.Type));

                    jORole.Add(new JProperty("primary", scimUserRole.Primary));

                    jORoles.Add(jORole);

                }
                jObject.Add(new JProperty("roles", jORoles));
            }

            if (scimUser.X509Certificates.Count() > 0)
            {
                var jOX509Certificates = new JArray();
                foreach (var scimUserX509Certificate in scimUser.X509Certificates)
                {
                    var JOX509Certificate = new JObject();

                    if (scimUserX509Certificate.Value != null)
                        JOX509Certificate.Add(new JProperty("value", scimUserX509Certificate.Value));

                    if (scimUserX509Certificate.Display != null)
                        JOX509Certificate.Add(new JProperty("display", scimUserX509Certificate.Display));

                    if (scimUserX509Certificate.Type != null)
                        JOX509Certificate.Add(new JProperty("type", scimUserX509Certificate.Type));
                    
                    JOX509Certificate.Add(new JProperty("primary", scimUserX509Certificate.Primary));

                    jOX509Certificates.Add(JOX509Certificate);

                }
                jObject.Add(new JProperty("x509Certificates", jOX509Certificates));
            }

            if (scimUser.EnterpriseUser != null)
            {
                var jOEnterpriseUser = new JObject();

                if(scimUser.EnterpriseUser.EmployeeNumber != null)
                    jOEnterpriseUser.Add(new JProperty("employeeNumber", scimUser.EnterpriseUser.EmployeeNumber));

                if (scimUser.EnterpriseUser.CostCenter != null)
                    jOEnterpriseUser.Add(new JProperty("costCenter", scimUser.EnterpriseUser.CostCenter));

                if (scimUser.EnterpriseUser.Organization != null)
                    jOEnterpriseUser.Add(new JProperty("organization", scimUser.EnterpriseUser.Organization));

                if (scimUser.EnterpriseUser.Division != null)
                    jOEnterpriseUser.Add(new JProperty("division", scimUser.EnterpriseUser.Division));

                if (scimUser.EnterpriseUser.Department != null)
                    jOEnterpriseUser.Add(new JProperty("department", scimUser.EnterpriseUser.Department));

                if(scimUser.EnterpriseUser.Manager != null)
                {
                    var jOManager = new JObject();
                    if (scimUser.EnterpriseUser.Manager.Value != null)
                        jOManager.Add(new JProperty("value", scimUser.EnterpriseUser.Manager.Value));

                    if (scimUser.EnterpriseUser.Manager.Ref != null)
                        jOManager.Add(new JProperty("$ref", scimUser.EnterpriseUser.Manager.Ref));

                    if (scimUser.EnterpriseUser.Manager.DisplayName != null)
                        jOManager.Add(new JProperty("displayName", scimUser.EnterpriseUser.Manager.DisplayName));
                    
                    jOEnterpriseUser.Add(new JProperty("manager", jOManager));
                }


                jObject.Add(new JProperty("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", jOEnterpriseUser));
            }


            if (scimUser.Meta != null)
            {
                var jOMeta = new JObject();
                if (scimUser.Meta.ResourceType != null)
                    jOMeta.Add(new JProperty("resourceType", scimUser.Meta.ResourceType));

                if (scimUser.Meta.Created != null)
                    jOMeta.Add(new JProperty("created", scimUser.Meta.Created));

                if (scimUser.Meta.LastModified != null)
                    jOMeta.Add(new JProperty("lastModified", scimUser.Meta.LastModified));

                if (scimUser.Meta.Version != null)
                    jOMeta.Add(new JProperty("version", scimUser.Meta.Version));

                if (scimUser.Meta.Location != null)
                    jOMeta.Add(new JProperty("location", scimUser.Meta.Location));

                jObject.Add(new JProperty("meta", jOMeta));
            }


            return jObject;
        }
        private async Task<IList<ScimUser>> FilterScimUsersCoreAsync(string filter)
        {
            var attribute = filter.Split(" ")[0];
            var operatorWord = filter.Split(" ")[1];
            var value = filter.Split(" ")[2].Replace("\"", "");
            IList<ScimUser> scimUsers = new List<ScimUser>();

            switch (operatorWord)
            {
                case "eq":
                    switch (attribute)
                    {
                        case "userName":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.UserName == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "externalId":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.ExternalId == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "name.formatted":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Name.Formatted == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "name.familyName":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Name.FamilyName == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "name.givenName":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Name.GivenName == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "name.middleName":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Name.MiddleName == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "name.honorificPrefix":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Name.HonorificPrefix == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync(); ;
                            break;
                        case "name.honorificSuffix":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Name.HonorificSuffix == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "displayName":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.DisplayName == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync(); ;
                            break;
                        case "nickName":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.NickName == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "profileUrl":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.ProfileUrl == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "title":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Title == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "userType":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.UserType == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "preferredLanguage":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.PreferredLanguage == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "locale":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Locale == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "timezone":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.TimeZone == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                        case "active":
                            if (value == "true")
                            {
                                scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Active == true)
                                    .Include(c => c.Name)
                                    .Include(c => c.Emails)
                                    .Include(c => c.Addresses)
                                    .Include(c => c.PhoneNumbers)
                                    .Include(c => c.Ims)
                                    .Include(c => c.Photos)
                                    .Include(c => c.Groups)
                                    .Include(c => c.Entitlements)
                                    .Include(c => c.Roles)
                                    .Include(c => c.X509Certificates)
                                    .Include(c => c.EnterpriseUser)
                                    .Include(c => c.EnterpriseUser.Manager)
                                    .Include(c => c.Meta)
                                    .ToListAsync();
                            }
                            else
                            {
                                scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Active == false)
                                    .Include(c => c.Name)
                                    .Include(c => c.Emails)
                                    .Include(c => c.Addresses)
                                    .Include(c => c.PhoneNumbers)
                                    .Include(c => c.Ims)
                                    .Include(c => c.Photos)
                                    .Include(c => c.Groups)
                                    .Include(c => c.Entitlements)
                                    .Include(c => c.Roles)
                                    .Include(c => c.X509Certificates)
                                    .Include(c => c.EnterpriseUser)
                                    .Include(c => c.EnterpriseUser.Manager)
                                    .Include(c => c.Meta)
                                    .ToListAsync();
                            }
                            break;
                        case "password":
                            scimUsers = await _scimDataContext.ScimUsers.Where(c => c.Password == value)
                                .Include(c => c.Name)
                                .Include(c => c.Emails)
                                .Include(c => c.Addresses)
                                .Include(c => c.PhoneNumbers)
                                .Include(c => c.Ims)
                                .Include(c => c.Photos)
                                .Include(c => c.Groups)
                                .Include(c => c.Entitlements)
                                .Include(c => c.Roles)
                                .Include(c => c.X509Certificates)
                                .Include(c => c.EnterpriseUser)
                                .Include(c => c.EnterpriseUser.Manager)
                                .Include(c => c.Meta)
                                .ToListAsync();
                            break;
                    }
                    break;

            }
            return scimUsers;
        }
        private async Task<ScimUser> PutScimUserCoreAsync(Guid id, JObject jObject)
        {
            var scimUser = await _scimDataContext.ScimUsers.Where(c => c.ScimUserId == id)
                .Include(c => c.Name)
                .Include(c => c.Emails)
                .Include(c => c.Addresses)
                .Include(c => c.PhoneNumbers)
                .Include(c => c.Ims)
                .Include(c => c.Photos)
                .Include(c => c.Groups)
                .Include(c => c.Entitlements)
                .Include(c => c.Roles)
                .Include(c => c.X509Certificates)
                .Include(c => c.EnterpriseUser)
                .Include(c => c.EnterpriseUser.Manager)
                .Include(c => c.Meta)
                .FirstOrDefaultAsync();

            RemoveSpecificAttributeForScimUserByPath(scimUser, "schemas");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "userName");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "name");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "displayName");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "nickName");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "profileUrl");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "title");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "userType");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "preferredLanguage");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "locale");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "timezone");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "active");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "password");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "emails");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "phoneNumbers");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "ims");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "photos");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "addresses");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "groups");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "entitlements");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "roles");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "x509Certificates");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User");
            RemoveSpecificAttributeForScimUserByPath(scimUser, "externalId");

            AddOrUpdateAttributesForScimUserByJson(scimUser, jObject);
            AddOrUpdateAttributeForScimUserByPath(scimUser, jObject, "meta");

            var putScimUser = await _scimDataContext.ScimUsers.Where(c => c.ScimUserId == id)
                .Include(c => c.Name)
                .Include(c => c.Emails)
                .Include(c => c.Addresses)
                .Include(c => c.PhoneNumbers)
                .Include(c => c.Ims)
                .Include(c => c.Photos)
                .Include(c => c.Groups)
                .Include(c => c.Entitlements)
                .Include(c => c.Roles)
                .Include(c => c.X509Certificates)
                .Include(c => c.EnterpriseUser)
                .Include(c => c.EnterpriseUser.Manager)
                .Include(c => c.Meta)
                .FirstOrDefaultAsync();

            return putScimUser;
        }
        private async Task<ScimUser> PatchScimUserCoreAync(Guid id, JObject jObject)
        {

            if ((JArray)jObject["schemas"] == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };

            if ((JArray)jObject["Operations"] == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };
            var operationWord = (string)jObject["schemas"][0];
            if (operationWord != "urn:ietf:params:scim:api:messages:2.0:PatchOp")
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };

            var scimUser = await _scimDataContext.ScimUsers.Where(c => c.ScimUserId == id)
                .Include(c => c.Name)
                .Include(c => c.Emails)
                .Include(c => c.Addresses)
                .Include(c => c.PhoneNumbers)
                .Include(c => c.Ims)
                .Include(c => c.Photos)
                .Include(c => c.Groups)
                .Include(c => c.Entitlements)
                .Include(c => c.Roles)
                .Include(c => c.X509Certificates)
                .Include(c => c.EnterpriseUser)
                .Include(c => c.EnterpriseUser.Manager)
                .Include(c => c.Meta)
                .FirstOrDefaultAsync();

            if (scimUser == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.NotFound };

            var operations = (JArray)jObject["Operations"];
            foreach (var operationJObject in operations)
            {
                string op = null;
                string path = null;

                if ((string)operationJObject["op"] != null)
                    op = (string)operationJObject["op"];
                if ((string)operationJObject["path"] != null)
                    path = (string)operationJObject["path"];

                if (op == "add" || op == "replace" || op == "Add" || op == "Replace")
                {
                    if (path == null)
                    {
                        if (op == "replace" || op == "Replace")
                            RemoveSpecificAttributeForScimUserByJson(scimUser, (JObject)operationJObject["value"]);

                        AddOrUpdateAttributesForScimUserByJson(scimUser, (JObject)operationJObject["value"]);
                    }
                    else
                    {
                        if (op == "replace" || op == "Replace")
                            RemoveSpecificAttributeForScimUserByPath(scimUser, path);
                        AddOrUpdateAttributeForScimUserByPath(scimUser, (JObject)operationJObject, path);
                    }
                }
                else if(op == "remove" || op == "Remove")
                {
                    RemoveSpecificAttributeForScimUserByPath(scimUser, path);
                }
            }


            AddOrUpdateAttributeForScimUserByPath(scimUser, jObject, "meta");

            var patchedScimUser = _scimDataContext.ScimUsers.Where(c => c.ScimUserId == id).FirstOrDefault();

            return scimUser;
        }
        private void AddOrUpdateAttributesForScimUserByJson(ScimUser scimUser, JObject jObject)
        {
            if ((JArray)jObject["schemas"] != null)
            {
                var schemaString = new StringBuilder();
                for (var i = 0; i < (int)jObject["schemas"].Count(); i++)
                {
                    schemaString.Append(jObject["schemas"][i]);

                    if ((i + 1) != (int)jObject["schemas"].Count())
                        schemaString.Append(",");
                }
                scimUser.Schemas = schemaString.ToString();
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["externalId"] != null)
            {
                scimUser.ExternalId = (string)jObject["externalId"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["userName"] != null)
            {
                scimUser.UserName = (string)jObject["userName"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((JObject)jObject["name"] != null)
            {
                if (scimUser.Name != null)
                {
                    if ((string)jObject["name"]["formatted"] != null)
                        scimUser.Name.Formatted = (string)jObject["name"]["formatted"];
                    if ((string)jObject["name"]["familyName"] != null)
                        scimUser.Name.FamilyName = (string)jObject["name"]["familyName"];
                    if ((string)jObject["name"]["givenName"] != null)
                        scimUser.Name.GivenName = (string)jObject["name"]["givenName"];
                    if ((string)jObject["name"]["middleName"] != null)
                        scimUser.Name.MiddleName = (string)jObject["name"]["middleName"];
                    if ((string)jObject["name"]["honorificPrefix"] != null)
                        scimUser.Name.HonorificPrefix = (string)jObject["name"]["honorificPrefix"];
                    if ((string)jObject["name"]["honorificSuffix"] != null)
                        scimUser.Name.HonorificSuffix = (string)jObject["name"]["honorificSuffix"];

                    _scimDataContext.ScimUsers.Update(scimUser);
                }
                else
                {
                    var scimUserName = new ScimUserName { };
                    if ((string)jObject["name"]["formatted"] != null)
                        scimUserName.Formatted = (string)jObject["name"]["formatted"];
                    if ((string)jObject["name"]["familyName"] != null)
                        scimUserName.FamilyName = (string)jObject["name"]["familyName"];
                    if ((string)jObject["name"]["givenName"] != null)
                        scimUserName.GivenName = (string)jObject["name"]["givenName"];
                    if ((string)jObject["name"]["middleName"] != null)
                        scimUserName.MiddleName = (string)jObject["name"]["middleName"];
                    if ((string)jObject["name"]["honorificPrefix"] != null)
                        scimUserName.HonorificPrefix = (string)jObject["name"]["honorificPrefix"];
                    if ((string)jObject["name"]["honorificSuffix"] != null)
                        scimUserName.HonorificSuffix = (string)jObject["name"]["honorificSuffix"];

                    scimUserName.ScimUser = scimUser;
                    _scimDataContext.ScimUserNames.Add(scimUserName);

                }
            }
            if ((string)jObject["name.givenName"] != null)
            {
                if (scimUser.Name != null)
                {
                    scimUser.Name.GivenName = (string)jObject["name.givenName"];
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
                else
                {
                    var scimUserName = new ScimUserName { };
                    scimUserName.GivenName = (string)jObject["name.givenName"];
                    scimUserName.ScimUser = scimUser;
                    _scimDataContext.ScimUserNames.Add(scimUserName);

                }
            }
            if ((string)jObject["name.familyName"] != null)
            {
                if (scimUser.Name != null)
                {
                    scimUser.Name.FamilyName = (string)jObject["name.familyName"];
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
                else
                {
                    var scimUserName = new ScimUserName { };
                    scimUserName.FamilyName = (string)jObject["name.familyName"];
                    scimUserName.ScimUser = scimUser;
                    _scimDataContext.ScimUserNames.Add(scimUserName);

                }
            }
            if ((string)jObject["name.formatted"] != null)
            {
                if (scimUser.Name != null)
                {
                    scimUser.Name.Formatted = (string)jObject["name.formatted"];
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
                else
                {
                    var scimUserName = new ScimUserName { };
                    scimUserName.Formatted = (string)jObject["name.formatted"];
                    scimUserName.ScimUser = scimUser;
                    _scimDataContext.ScimUserNames.Add(scimUserName);

                }
            }
            if ((string)jObject["displayName"] != null)
            {
                _scimDataContext.ScimUsers.Update(scimUser);
                scimUser.DisplayName = (string)jObject["displayName"];
            }
            if ((string)jObject["nickName"] != null)
            {
                scimUser.NickName = (string)jObject["nickName"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["profileUrl"] != null)
            {
                scimUser.ProfileUrl = (string)jObject["profileUrl"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["title"] != null)
            {
                scimUser.Title = (string)jObject["title"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["userType"] != null)
            {
                scimUser.UserType = (string)jObject["userType"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["preferredLanguage"] != null)
            {
                scimUser.PreferredLanguage = (string)jObject["preferredLanguage"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["locale"] != null)
            {
                scimUser.Locale = (string)jObject["locale"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["timezone"] != null)
            {
                _scimDataContext.ScimUsers.Update(scimUser);
                scimUser.TimeZone = (string)jObject["timezone"];
            }
            if ((string)jObject["active"] != null)
            {
                scimUser.Active = (bool)jObject["active"];
                _scimDataContext.ScimUsers.Update(scimUser);
            }
            if ((string)jObject["password"] != null)
            {
                _scimDataContext.ScimUsers.Update(scimUser);
                scimUser.Password = (string)jObject["password"];
            }
            if ((JArray)jObject["emails"] != null)
            {
                foreach (var email in (JArray)jObject["emails"])
                {
                    var scimUserEmail = new ScimUserEmail();

                    if ((string)email["value"] != null)
                    {
                        var tmpEmail = (string)email["value"];
                        var dupScimUserEmail = _scimDataContext.ScimUserEmails.Where(c => c.Value == tmpEmail).FirstOrDefault();

                        if((dupScimUserEmail != null) && (scimUser.ScimUserId != dupScimUserEmail.ScimUserId))
                        {
                            throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "uniqueness" };
                        }
                        scimUserEmail.Value = (string)email["value"];
                    }
                    if ((string)email["display"] != null)
                        scimUserEmail.Display = (string)email["display"];
                    if ((string)email["type"] != null)
                        scimUserEmail.Type = (string)email["type"];
                    if ((string)email["primary"] != null)
                        scimUserEmail.Primary = (bool)email["primary"];

                    scimUserEmail.ScimUser = scimUser;
                    _scimDataContext.ScimUserEmails.Add(scimUserEmail);
                }
            }
            if ((JArray)jObject["phoneNumbers"] != null)
            {
                foreach (var phoneNumber in (JArray)jObject["phoneNumbers"])
                {
                    var scimUserPhoneNumber = new ScimUserPhoneNumber();

                    if ((string)phoneNumber["value"] != null)
                        scimUserPhoneNumber.Value = (string)phoneNumber["value"];
                    if ((string)phoneNumber["display"] != null)
                        scimUserPhoneNumber.Display = (string)phoneNumber["display"];
                    if ((string)phoneNumber["type"] != null)
                        scimUserPhoneNumber.Type = (string)phoneNumber["type"];
                    if ((string)phoneNumber["primary"] != null)
                        scimUserPhoneNumber.Primary = (bool)phoneNumber["primary"];

                    scimUserPhoneNumber.ScimUser = scimUser;
                    _scimDataContext.ScimUserPhoneNumbers.Add(scimUserPhoneNumber);
                }
            }
            if ((JArray)jObject["ims"] != null)
            {
                foreach (var im in (JArray)jObject["ims"])
                {
                    var scimUserIm = new ScimUserIm();

                    if ((string)im["value"] != null)
                        scimUserIm.Value = (string)im["value"];
                    if ((string)im["display"] != null)
                        scimUserIm.Display = (string)im["display"];
                    if ((string)im["type"] != null)
                        scimUserIm.Type = (string)im["type"];
                    if ((string)im["primary"] != null)
                        scimUserIm.Primary = (bool)im["primary"];

                    scimUserIm.ScimUser = scimUser;
                    _scimDataContext.ScimUserIms.Add(scimUserIm);
                }
            }
            if ((JArray)jObject["photos"] != null)
            {
                foreach (var photo in (JArray)jObject["photos"])
                {
                    var scimUserPhoto = new ScimUserPhoto();

                    if ((string)photo["value"] != null)
                        scimUserPhoto.Value = (string)photo["value"];
                    if ((string)photo["display"] != null)
                        scimUserPhoto.Display = (string)photo["display"];
                    if ((string)photo["type"] != null)
                        scimUserPhoto.Type = (string)photo["type"];
                    if ((string)photo["primary"] != null)
                        scimUserPhoto.Primary = (bool)photo["primary"];

                    scimUserPhoto.ScimUser = scimUser;
                    _scimDataContext.ScimUserPhotos.Add(scimUserPhoto);
                }
            }
            if ((JArray)jObject["addresses"] != null)
            {
                foreach (var address in (JArray)jObject["addresses"])
                {
                    var scimUserAddress = new ScimUserAddress();

                    if ((string)address["formatted"] != null)
                        scimUserAddress.Formatted = (string)address["formatted"];
                    if ((string)address["streetAddress"] != null)
                        scimUserAddress.StreetAddress = (string)address["streetAddress"];
                    if ((string)address["locality"] != null)
                        scimUserAddress.Locality = (string)address["locality"];
                    if ((string)address["region"] != null)
                        scimUserAddress.Region = (string)address["region"];
                    if ((string)address["postalCode"] != null)
                        scimUserAddress.PostalCode = (string)address["postalCode"];
                    if ((string)address["country"] != null)
                        scimUserAddress.Country = (string)address["country"];
                    if ((string)address["type"] != null)
                        scimUserAddress.Locality = (string)address["type"];

                    scimUserAddress.ScimUser = scimUser;
                    _scimDataContext.ScimUserAddresses.Add(scimUserAddress);
                }
            }
            if ((JArray)jObject["groups"] != null)
            {
                foreach (var group in (JArray)jObject["groups"])
                {
                    var scimUserGroup = new ScimUserGroup();

                    if ((string)group["value"] != null)
                        scimUserGroup.Value = (string)group["value"];
                    if ((string)group["$re"] != null)
                        scimUserGroup.Ref = (string)group["$re"];
                    if ((string)group["display"] != null)
                        scimUserGroup.Display = (string)group["display"];
                    if ((string)group["type"] != null)
                        scimUserGroup.Type = (string)group["type"];

                    scimUserGroup.ScimUser = scimUser;
                    _scimDataContext.ScimUserGroups.Add(scimUserGroup);
                }
            }
            if ((JArray)jObject["entitlements"] != null)
            {
                foreach (var entitlement in (JArray)jObject["entitlements"])
                {
                    var scimUserEntitlement = new ScimUserEntitlement();

                    if ((string)entitlement["value"] != null)
                        scimUserEntitlement.Value = (string)entitlement["value"];
                    if ((string)entitlement["display"] != null)
                        scimUserEntitlement.Display = (string)entitlement["display"];
                    if ((string)entitlement["type"] != null)
                        scimUserEntitlement.Type = (string)entitlement["type"];
                    if ((string)entitlement["primary"] != null)
                        scimUserEntitlement.Primary = (bool)entitlement["primary"];

                    scimUserEntitlement.ScimUser = scimUser;
                    _scimDataContext.ScimUserEntitlements.Add(scimUserEntitlement);
                }

            }
            if ((JArray)jObject["roles"] != null)
            {
                foreach (var role in (JArray)jObject["roles"])
                {
                    var scimUserRole = new ScimUserRole();

                    if ((string)role["value"] != null)
                        scimUserRole.Value = (string)role["value"];
                    if ((string)role["display"] != null)
                        scimUserRole.Display = (string)role["display"];
                    if ((string)role["type"] != null)
                        scimUserRole.Type = (string)role["type"];
                    if ((string)role["primary"] != null)
                        scimUserRole.Primary = (bool)role["primary"];

                    scimUserRole.ScimUser = scimUser;
                    _scimDataContext.ScimUserRoles.Add(scimUserRole);
                }
            }
            if ((JArray)jObject["x509Certificates"] != null)
            {
                foreach (var x509Certificate in (JArray)jObject["x509Certificates"])
                {
                    var scimUserX509Certificate = new ScimUserX509Certificate();

                    if ((string)x509Certificate["value"] != null)
                        scimUserX509Certificate.Value = (string)x509Certificate["value"];
                    if ((string)x509Certificate["display"] != null)
                        scimUserX509Certificate.Display = (string)x509Certificate["display"];
                    if ((string)x509Certificate["type"] != null)
                        scimUserX509Certificate.Type = (string)x509Certificate["type"];
                    if ((string)x509Certificate["primary"] != null)
                        scimUserX509Certificate.Primary = (bool)x509Certificate["primary"];

                    scimUserX509Certificate.ScimUser = scimUser;
                    _scimDataContext.ScimUserX509Certificates.Add(scimUserX509Certificate);
                }
            }
            if ((JObject)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"] != null)
            {
                if (scimUser.EnterpriseUser != null)
                {

                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["employeeNumber"] != null)
                        scimUser.EnterpriseUser.EmployeeNumber = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["employeeNumber"];
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["costCenter"] != null)
                        scimUser.EnterpriseUser.CostCenter = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["costCenter"];
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["organization"] != null)
                        scimUser.EnterpriseUser.Organization = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["organization"];
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["division"] != null)
                        scimUser.EnterpriseUser.Division = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["division"];
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["department"] != null)
                        scimUser.EnterpriseUser.Department = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["department"];

                    _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);

                    if ((JObject)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"] != null)
                    {
                        if (scimUser.EnterpriseUser.Manager != null)
                        {
                            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["value"] != null)
                                scimUser.EnterpriseUser.Manager.Value = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["value"];
                            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["$ref"] != null)
                                scimUser.EnterpriseUser.Manager.Ref = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["$ref"];
                            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["displayName"] != null)
                                scimUser.EnterpriseUser.Manager.DisplayName = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["displayName"];
                            _scimDataContext.ScimUserManagers.Update(scimUser.EnterpriseUser.Manager);
                        }
                        else
                        {
                            var scimUserManager = new ScimUserManager { };
                            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["value"] != null)
                                scimUserManager.Value = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["value"];
                            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["$ref"] != null)
                                scimUserManager.Ref = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["$ref"];
                            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["displayName"] != null)
                                scimUserManager.DisplayName = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["displayName"];

                            scimUserManager.ScimUserEnterpriseUser = scimUser.EnterpriseUser;
                            _scimDataContext.ScimUserManagers.Add(scimUserManager);
                        }
                    }
                }
                else
                {
                    if (!scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                        scimUser.Schemas = scimUser.Schemas.ToString() + ",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

                    var scimUserEnterpriseUser = new ScimUserEnterpriseUser { };
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["employeeNumber"] != null)
                        scimUserEnterpriseUser.EmployeeNumber = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["employeeNumber"];
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["costCenter"] != null)
                        scimUserEnterpriseUser.CostCenter = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["costCenter"];
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["organization"] != null)
                        scimUserEnterpriseUser.Organization = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["organization"];
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["division"] != null)
                        scimUserEnterpriseUser.Division = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["division"];
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["department"] != null)
                        scimUserEnterpriseUser.Department = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["department"];
                    if ((JObject)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"] != null)
                    {
                        var scimUserManager = new ScimUserManager { };
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["value"] != null)
                            scimUserManager.Value = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["value"];
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["$ref"] != null)
                            scimUserManager.Ref = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["$ref"];
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["displayName"] != null)
                            scimUserManager.DisplayName = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"]["manager"]["displayName"];

                        scimUserManager.ScimUserEnterpriseUser = scimUserEnterpriseUser;
                        _scimDataContext.ScimUserManagers.Add(scimUserManager);
                    }
                    scimUserEnterpriseUser.ScimUser = scimUser;
                    _scimDataContext.ScimUserEnterpriseUsers.Add(scimUserEnterpriseUser);
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((JObject)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"] != null)
            {
                if (scimUser.EnterpriseUser != null)
                {

                    if (scimUser.EnterpriseUser.Manager != null)
                    {
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["value"] != null)
                            scimUser.EnterpriseUser.Manager.Value = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["value"];
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["$ref"] != null)
                            scimUser.EnterpriseUser.Manager.Ref = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["$ref"];
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["displayName"] != null)
                            scimUser.EnterpriseUser.Manager.DisplayName = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["displayName"];
                        _scimDataContext.ScimUserManagers.Update(scimUser.EnterpriseUser.Manager);
                    }
                    else
                    {
                        var scimUserManager = new ScimUserManager { };
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["value"] != null)
                            scimUserManager.Value = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["value"];
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["$ref"] != null)
                            scimUserManager.Ref = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["$ref"];
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["displayName"] != null)
                            scimUserManager.DisplayName = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["displayName"];

                        scimUserManager.ScimUserEnterpriseUser = scimUser.EnterpriseUser;
                        _scimDataContext.ScimUserManagers.Add(scimUserManager);
                    }

                }
                else
                {
                    if (!scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                        scimUser.Schemas = scimUser.Schemas.ToString() + ",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

                    var scimUserEnterpriseUser = new ScimUserEnterpriseUser { };
                    if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"] != null)
                    {
                        var scimUserManager = new ScimUserManager { };
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["value"] != null)
                            scimUserManager.Value = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["value"];
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["$ref"] != null)
                            scimUserManager.Ref = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["$ref"];
                        if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["manager"]["displayName"] != null)
                            scimUserManager.DisplayName = (string)jObject["uurn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager"]["displayName"];

                        scimUserManager.ScimUserEnterpriseUser = scimUserEnterpriseUser;
                        _scimDataContext.ScimUserManagers.Add(scimUserManager);
                    }
                    scimUserEnterpriseUser.ScimUser = scimUser;
                    _scimDataContext.ScimUserEnterpriseUsers.Add(scimUserEnterpriseUser);
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }

            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber"] != null)
            {
                if (scimUser.EnterpriseUser != null)
                    scimUser.EnterpriseUser.EmployeeNumber = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber"];
                _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);
            }

            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department"] != null)
            {
                if (scimUser.EnterpriseUser != null)
                    scimUser.EnterpriseUser.Department = (string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department"];
                _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);
            }

            _scimDataContext.SaveChanges();
        }
        private void AddOrUpdateAttributeForScimUserByPath(ScimUser scimUser, JObject jObject, string path)
        {
            switch (path)
            {
                case "userName":
                    scimUser.UserName = (string)jObject["value"];
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "name.formatted":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.Formatted = (string)jObject["value"];
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserName = new ScimUserName { };
                        scimUserName.Formatted = (string)jObject["value"];
                        scimUserName.ScimUser = scimUser;
                        _scimDataContext.ScimUserNames.Add(scimUserName);
                        _scimDataContext.SaveChanges();

                    }
                    break;
                case "name.familyName":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.FamilyName = (string)jObject["value"];
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserName = new ScimUserName { };
                        scimUserName.FamilyName = (string)jObject["value"];
                        scimUserName.ScimUser = scimUser;
                        _scimDataContext.ScimUserNames.Add(scimUserName);
                        _scimDataContext.SaveChanges();

                    }
                    break;
                case "name.givenName":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.GivenName = (string)jObject["value"];
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserName = new ScimUserName { };
                        scimUserName.GivenName = (string)jObject["value"];
                        scimUserName.ScimUser = scimUser;
                        _scimDataContext.ScimUserNames.Add(scimUserName);
                        _scimDataContext.SaveChanges();

                    }
                    break;
                case "name.middleName":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.MiddleName = (string)jObject["value"];
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserName = new ScimUserName { };
                        scimUserName.MiddleName = (string)jObject["value"];
                        scimUserName.ScimUser = scimUser;
                        _scimDataContext.ScimUserNames.Add(scimUserName);
                    }
                    break;
                case "name.honorificPrefix":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.HonorificPrefix = (string)jObject["value"];
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserName = new ScimUserName { };
                        scimUserName.HonorificPrefix = (string)jObject["value"];
                        scimUserName.ScimUser = scimUser;
                        _scimDataContext.ScimUserNames.Add(scimUserName);
                        _scimDataContext.SaveChanges();

                    }
                    break;
                case "name.honorificSuffix":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.HonorificPrefix = (string)jObject["value"];
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserName = new ScimUserName { };
                        scimUserName.HonorificSuffix = (string)jObject["value"];
                        scimUserName.ScimUser = scimUser;
                        _scimDataContext.ScimUserNames.Add(scimUserName);
                        _scimDataContext.SaveChanges();

                    }
                    break;

                case "displayName":
                    scimUser.DisplayName = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "nickName":
                    scimUser.NickName = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "profileUrl":
                    scimUser.ProfileUrl = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "title":
                    scimUser.Title = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "userType":
                    scimUser.UserType = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "preferredLanguage":
                    scimUser.PreferredLanguage = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "locale":
                    scimUser.Locale = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "timezone":
                    scimUser.TimeZone = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "active":
                    scimUser.Active = (bool)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;
                case "password":
                    scimUser.Password = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;

                case "emails":
                    var emails = (JArray)jObject["value"];
                    foreach (var email in emails)
                    {
                        var scimUserEmail = new ScimUserEmail { };
                        if ((string)email["value"] != null)
                            scimUserEmail.Value = (string)email["value"];
                        if ((string)email["display"] != null)
                            scimUserEmail.Display = (string)email["display"];
                        if ((string)email["type"] != null)
                            scimUserEmail.Type = (string)email["type"];
                        if ((string)email["primary"] != null)
                            scimUserEmail.Primary = (bool)email["primary"];
                        scimUserEmail.ScimUser = scimUser;

                        _scimDataContext.ScimUserEmails.Add(scimUserEmail);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "emails[type eq \"work\"].value":
                    if (scimUser.Emails.Count > 0)
                    {
                        var scimUserWorkEmail = scimUser.Emails.Where(c => c.Type == "work").FirstOrDefault();
                        if (scimUserWorkEmail != null)
                        {
                            scimUserWorkEmail.Value = (string)jObject["value"];
                            _scimDataContext.ScimUserEmails.Update(scimUserWorkEmail);
                            _scimDataContext.SaveChanges();
                        }
                        else
                        {
                            var scimUserEmail = new ScimUserEmail { };
                            scimUserEmail.Type = "work";
                            scimUserEmail.Value = (string)jObject["value"];
                            scimUserEmail.ScimUser = scimUser;
                            _scimDataContext.ScimUserEmails.Add(scimUserEmail);
                            _scimDataContext.SaveChanges();
                        }

                    }
                    else
                    {
                        var scimUserEmail = new ScimUserEmail { };
                        scimUserEmail.Type = "work";
                        scimUserEmail.Value = (string)jObject["value"];
                        scimUserEmail.ScimUser = scimUser;
                        _scimDataContext.ScimUserEmails.Add(scimUserEmail);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "phoneNumbers":
                    var phoneNumbers = (JArray)jObject["value"];
                    foreach (var phoneNumber in phoneNumbers)
                    {
                        var scimUserPhoneNumber = new ScimUserPhoneNumber { };
                        if ((string)phoneNumber["value"] != null)
                            scimUserPhoneNumber.Value = (string)phoneNumber["value"];
                        if ((string)phoneNumber["display"] != null)
                            scimUserPhoneNumber.Display = (string)phoneNumber["display"];
                        if ((string)phoneNumber["type"] != null)
                            scimUserPhoneNumber.Type = (string)phoneNumber["type"];
                        if ((string)phoneNumber["primary"] != null)
                            scimUserPhoneNumber.Primary = (bool)phoneNumber["primary"];
                        scimUserPhoneNumber.ScimUser = scimUser;

                        _scimDataContext.ScimUserPhoneNumbers.Add(scimUserPhoneNumber);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "phoneNumbers[type eq \"work\"].value":
                    var phoneNumberforwork = (string)jObject["value"];

                    var scimUserPhoneNumbersforwork = scimUser.PhoneNumbers.Where(c => c.Type == "work").ToList();
                    if(scimUserPhoneNumbersforwork.Count > 0)
                    {
                        foreach(var scimUserPhoneNumbersforworkItem in scimUserPhoneNumbersforwork)
                        {
                            scimUserPhoneNumbersforworkItem.Value = phoneNumberforwork;
                            _scimDataContext.ScimUserPhoneNumbers.Update(scimUserPhoneNumbersforworkItem);
                            _scimDataContext.SaveChanges();

                        }
                    }
                    else
                    {
                        var scimUserPhoneNumber = new ScimUserPhoneNumber { };
                        scimUserPhoneNumber.Value = phoneNumberforwork;
                        scimUserPhoneNumber.Type = "work";
                        scimUserPhoneNumber.ScimUser = scimUser;
                        _scimDataContext.ScimUserPhoneNumbers.Add(scimUserPhoneNumber);                     
                        _scimDataContext.SaveChanges();

                    }

                    break;

                case "phoneNumbers[type eq \"mobile\"].value":
                    var phoneNumberformobile = (string)jObject["value"];

                    var scimUserPhoneNumbersformobile = scimUser.PhoneNumbers.Where(c => c.Type == "mobile").ToList();
                    if (scimUserPhoneNumbersformobile.Count > 0)
                    {
                        foreach (var scimUserPhoneNumbersformobileItem in scimUserPhoneNumbersformobile)
                        {
                            scimUserPhoneNumbersformobileItem.Value = phoneNumberformobile;
                            _scimDataContext.ScimUserPhoneNumbers.Update(scimUserPhoneNumbersformobileItem);
                            _scimDataContext.SaveChanges();

                        }
                    }
                    else
                    {
                        var scimUserPhoneNumber = new ScimUserPhoneNumber { };
                        scimUserPhoneNumber.Value = phoneNumberformobile;
                        scimUserPhoneNumber.Type = "mobile";
                        scimUserPhoneNumber.ScimUser = scimUser;
                        _scimDataContext.ScimUserPhoneNumbers.Add(scimUserPhoneNumber);
                        _scimDataContext.SaveChanges();

                    }

                    break;

                case "phoneNumbers[type eq \"fax\"].value":
                    var phoneNumberforfax = (string)jObject["value"];

                    var scimUserPhoneNumbersforfax = scimUser.PhoneNumbers.Where(c => c.Type == "fax").ToList();
                    if (scimUserPhoneNumbersforfax.Count > 0)
                    {
                        foreach (var scimUserPhoneNumbersforfaxItem in scimUserPhoneNumbersforfax)
                        {
                            scimUserPhoneNumbersforfaxItem.Value = phoneNumberforfax;
                            _scimDataContext.ScimUserPhoneNumbers.Update(scimUserPhoneNumbersforfaxItem);
                            _scimDataContext.SaveChanges();

                        }
                    }
                    else
                    {
                        var scimUserPhoneNumber = new ScimUserPhoneNumber { };
                        scimUserPhoneNumber.Value = phoneNumberforfax;
                        scimUserPhoneNumber.Type = "fax";
                        scimUserPhoneNumber.ScimUser = scimUser;
                        _scimDataContext.ScimUserPhoneNumbers.Add(scimUserPhoneNumber);
                        _scimDataContext.SaveChanges();

                    }
                    break;
                case "ims":
                    var ims = (JArray)jObject["value"];
                    foreach (var im in ims)
                    {
                        var scimUserIm = new ScimUserIm { };
                        if ((string)im["value"] != null)
                            scimUserIm.Value = (string)im["value"];
                        if ((string)im["display"] != null)
                            scimUserIm.Display = (string)im["display"];
                        if ((string)im["type"] != null)
                            scimUserIm.Type = (string)im["type"];
                        if ((string)im["primary"] != null)
                            scimUserIm.Primary = (bool)im["primary"];
                        scimUserIm.ScimUser = scimUser;

                        _scimDataContext.ScimUserIms.Add(scimUserIm);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "photos":
                    var photos = (JArray)jObject["value"];
                    foreach (var photo in photos)
                    {
                        var scimUserPhoto = new ScimUserPhoto { };
                        if ((string)photo["value"] != null)
                            scimUserPhoto.Value = (string)photo["value"];
                        if ((string)photo["display"] != null)
                            scimUserPhoto.Display = (string)photo["display"];
                        if ((string)photo["type"] != null)
                            scimUserPhoto.Type = (string)photo["type"];
                        if ((string)photo["primary"] != null)
                            scimUserPhoto.Primary = (bool)photo["primary"];
                        scimUserPhoto.ScimUser = scimUser;

                        _scimDataContext.ScimUserPhotos.Add(scimUserPhoto);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "addresses":
                    var addresses = (JArray)jObject["value"];
                    foreach (var address in addresses)
                    {
                        var scimUserAddress = new ScimUserAddress { };
                        if ((string)address["formatted"] != null)
                            scimUserAddress.Formatted = (string)address["formatted"];
                        if ((string)address["streetAddress"] != null)
                            scimUserAddress.StreetAddress = (string)address["streetAddress"];
                        if ((string)address["locality"] != null)
                            scimUserAddress.Locality = (string)address["locality"];
                        if ((string)address["region"] != null)
                            scimUserAddress.Region = (string)address["region"];
                        if ((string)address["postalCode"] != null)
                            scimUserAddress.PostalCode = (string)address["postalCode"];
                        if ((string)address["country"] != null)
                            scimUserAddress.Country = (string)address["country"];
                        if ((string)address["type"] != null)
                            scimUserAddress.Type = (string)address["type"];

                        scimUserAddress.ScimUser = scimUser;

                        _scimDataContext.ScimUserAddresses.Add(scimUserAddress);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "addresses[type eq \"work\"].formatted":
                    var formatted = (string)jObject["value"];

                    var scimUserAddressListforformatted = scimUser.Addresses.Where(c => c.Type == "work").ToList();
                    if(scimUserAddressListforformatted.Count > 0)
                    {
                        foreach (var scimUserAddressItemforformatted in scimUserAddressListforformatted)
                        {
                            scimUserAddressItemforformatted.Formatted = formatted;
                            _scimDataContext.ScimUserAddresses.Update(scimUserAddressItemforformatted);
                            _scimDataContext.SaveChanges();
                        }

                    }
                    else
                    {
                        var scimUserAddressforformatted = new ScimUserAddress { };
                        scimUserAddressforformatted.Formatted = formatted;
                        scimUserAddressforformatted.Type = "work";
                        scimUserAddressforformatted.ScimUser = scimUser;

                        _scimDataContext.ScimUserAddresses.Add(scimUserAddressforformatted);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "addresses[type eq \"work\"].streetAddress":
                    var streetAddress = (string)jObject["value"];

                    var scimUserAddressListforstreetAddress = scimUser.Addresses.Where(c => c.Type == "work").ToList();
                    if (scimUserAddressListforstreetAddress.Count > 0)
                    {
                        foreach (var scimUserAddressItemforstreetAddress in scimUserAddressListforstreetAddress)
                        {
                            scimUserAddressItemforstreetAddress.StreetAddress = streetAddress;
                            _scimDataContext.ScimUserAddresses.Update(scimUserAddressItemforstreetAddress);
                            _scimDataContext.SaveChanges();
                        }

                    }
                    else
                    {
                        var scimUserAddressforstreetAddress = new ScimUserAddress { };
                        scimUserAddressforstreetAddress.StreetAddress = streetAddress;
                        scimUserAddressforstreetAddress.Type = "work";
                        scimUserAddressforstreetAddress.ScimUser = scimUser;

                        _scimDataContext.ScimUserAddresses.Add(scimUserAddressforstreetAddress);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "addresses[type eq \"work\"].locality":
                    var locality = (string)jObject["value"];

                    var scimUserAddressListforlocality = scimUser.Addresses.Where(c => c.Type == "work").ToList();
                    if (scimUserAddressListforlocality.Count > 0)
                    {
                        foreach (var scimUserAddressItemforlocality in scimUserAddressListforlocality)
                        {
                            scimUserAddressItemforlocality.Locality = locality;
                            _scimDataContext.ScimUserAddresses.Update(scimUserAddressItemforlocality);
                            _scimDataContext.SaveChanges();
                        }

                    }
                    else
                    {
                        var scimUserAddressforlocality = new ScimUserAddress { };
                        scimUserAddressforlocality.Locality = locality;
                        scimUserAddressforlocality.Type = "work";
                        scimUserAddressforlocality.ScimUser = scimUser;

                        _scimDataContext.ScimUserAddresses.Add(scimUserAddressforlocality);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "addresses[type eq \"work\"].region":
                    var region = (string)jObject["value"];

                    var scimUserAddressListforregion = scimUser.Addresses.Where(c => c.Type == "work").ToList();
                    if (scimUserAddressListforregion.Count > 0)
                    {
                        foreach (var scimUserAddressItemforregion in scimUserAddressListforregion)
                        {
                            scimUserAddressItemforregion.Region = region;
                            _scimDataContext.ScimUserAddresses.Update(scimUserAddressItemforregion);
                            _scimDataContext.SaveChanges();
                        }

                    }
                    else
                    {
                        var scimUserAddressforregion = new ScimUserAddress { };
                        scimUserAddressforregion.Region = region;
                        scimUserAddressforregion.Type = "work";
                        scimUserAddressforregion.ScimUser = scimUser;

                        _scimDataContext.ScimUserAddresses.Add(scimUserAddressforregion);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "addresses[type eq \"work\"].postalCode":
                    var postalCode = (string)jObject["value"];

                    var scimUserAddressListforpostalCode = scimUser.Addresses.Where(c => c.Type == "work").ToList();
                    if (scimUserAddressListforpostalCode.Count > 0)
                    {
                        foreach (var scimUserAddressItemforpostalCode in scimUserAddressListforpostalCode)
                        {
                            scimUserAddressItemforpostalCode.PostalCode = postalCode;
                            _scimDataContext.ScimUserAddresses.Update(scimUserAddressItemforpostalCode);
                            _scimDataContext.SaveChanges();
                        }

                    }
                    else
                    {
                        var scimUserAddressforpostalCode = new ScimUserAddress { };
                        scimUserAddressforpostalCode.PostalCode = postalCode;
                        scimUserAddressforpostalCode.Type = "work";
                        scimUserAddressforpostalCode.ScimUser = scimUser;

                        _scimDataContext.ScimUserAddresses.Add(scimUserAddressforpostalCode);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "addresses[type eq \"work\"].country":
                    var country = (string)jObject["value"];

                    var scimUserAddressListforcountry = scimUser.Addresses.Where(c => c.Type == "work").ToList();
                    if (scimUserAddressListforcountry.Count > 0)
                    {
                        foreach (var scimUserAddressItemforcountry in scimUserAddressListforcountry)
                        {
                            scimUserAddressItemforcountry.Country = country;
                            _scimDataContext.ScimUserAddresses.Update(scimUserAddressItemforcountry);
                            _scimDataContext.SaveChanges();
                        }

                    }
                    else
                    {
                        var scimUserAddressforcountry = new ScimUserAddress { };
                        scimUserAddressforcountry.Country = country;
                        scimUserAddressforcountry.Type = "work";
                        scimUserAddressforcountry.ScimUser = scimUser;

                        _scimDataContext.ScimUserAddresses.Add(scimUserAddressforcountry);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "groups":
                    var groups = (JArray)jObject["value"];
                    foreach (var group in groups)
                    {
                        var scimUserGroup = new ScimUserGroup { };
                        if ((string)group["value"] != null)
                            scimUserGroup.Value = (string)group["value"];
                        if ((string)group["$ref"] != null)
                            scimUserGroup.Ref = (string)group["$ref"];
                        if ((string)group["display"] != null)
                            scimUserGroup.Display = (string)group["display"];
                        if ((string)group["type"] != null)
                            scimUserGroup.Type = (string)group["type"];

                        scimUserGroup.ScimUser = scimUser;

                        _scimDataContext.ScimUserGroups.Add(scimUserGroup);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "entitlements":
                    var entitlements = (JArray)jObject["value"];
                    foreach (var entitlement in entitlements)
                    {
                        var scimUserEntitlement = new ScimUserEntitlement { };
                        if ((string)entitlement["value"] != null)
                            scimUserEntitlement.Value = (string)entitlement["value"];
                        if ((string)entitlement["display"] != null)
                            scimUserEntitlement.Display = (string)entitlement["display"];
                        if ((string)entitlement["type"] != null)
                            scimUserEntitlement.Type = (string)entitlement["type"];
                        if ((string)entitlement["primary"] != null)
                            scimUserEntitlement.Primary = (bool)entitlement["primary"];
                        scimUserEntitlement.ScimUser = scimUser;

                        _scimDataContext.ScimUserEntitlements.Add(scimUserEntitlement);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "roles":
                    var roles = (JArray)jObject["value"];
                    foreach (var role in roles)
                    {
                        var scimUserRole = new ScimUserRole { };
                        if ((string)role["value"] != null)
                            scimUserRole.Value = (string)role["value"];
                        if ((string)role["display"] != null)
                            scimUserRole.Display = (string)role["display"];
                        if ((string)role["type"] != null)
                            scimUserRole.Type = (string)role["type"];
                        if ((string)role["primary"] != null)
                            scimUserRole.Primary = (bool)role["primary"];
                        scimUserRole.ScimUser = scimUser;

                        _scimDataContext.ScimUserRoles.Add(scimUserRole);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "x509Certificates":
                    var x509Certificates = (JArray)jObject["value"];
                    foreach (var x509Certificate in x509Certificates)
                    {
                        var scimUserX509Certificate = new ScimUserX509Certificate { };
                        if ((string)x509Certificate["value"] != null)
                            scimUserX509Certificate.Value = (string)x509Certificate["value"];
                        if ((string)x509Certificate["display"] != null)
                            scimUserX509Certificate.Display = (string)x509Certificate["display"];
                        if ((string)x509Certificate["type"] != null)
                            scimUserX509Certificate.Type = (string)x509Certificate["type"];
                        if ((string)x509Certificate["primary"] != null)
                            scimUserX509Certificate.Primary = (bool)x509Certificate["primary"];
                        scimUserX509Certificate.ScimUser = scimUser;

                        _scimDataContext.ScimUserX509Certificates.Add(scimUserX509Certificate);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber":
                    if (scimUser.EnterpriseUser != null)
                    {
                        scimUser.EnterpriseUser.EmployeeNumber = (string)jObject["value"];

                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserEnterpriseUser = new ScimUserEnterpriseUser { };
                        scimUserEnterpriseUser.EmployeeNumber = (string)jObject["value"];
                        scimUserEnterpriseUser.ScimUser = scimUser;
                        scimUser.EnterpriseUser = scimUserEnterpriseUser;
                        if (!scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                            scimUser.Schemas = scimUser.Schemas.ToString() + ",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

                        _scimDataContext.ScimUserEnterpriseUsers.Add(scimUserEnterpriseUser);
                        _scimDataContext.ScimUsers.Update(scimUser);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter":
                    if (scimUser.EnterpriseUser != null)
                    {
                        scimUser.EnterpriseUser.CostCenter = (string)jObject["value"];

                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserEnterpriseUser = new ScimUserEnterpriseUser { };
                        scimUserEnterpriseUser.CostCenter = (string)jObject["value"];
                        scimUserEnterpriseUser.ScimUser = scimUser;
                        scimUser.EnterpriseUser = scimUserEnterpriseUser;
                        if (!scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                            scimUser.Schemas = scimUser.Schemas.ToString() + ",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

                        _scimDataContext.ScimUserEnterpriseUsers.Add(scimUserEnterpriseUser);
                        _scimDataContext.ScimUsers.Update(scimUser);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization":
                    if (scimUser.EnterpriseUser != null)
                    {
                        scimUser.EnterpriseUser.Organization = (string)jObject["value"];
                        
                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserEnterpriseUser = new ScimUserEnterpriseUser { };
                        scimUserEnterpriseUser.Organization = (string)jObject["value"];
                        scimUserEnterpriseUser.ScimUser = scimUser;
                        scimUser.EnterpriseUser = scimUserEnterpriseUser;
                        if (!scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                            scimUser.Schemas = scimUser.Schemas.ToString() + ",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

                        _scimDataContext.ScimUserEnterpriseUsers.Add(scimUserEnterpriseUser);
                        _scimDataContext.ScimUsers.Update(scimUser);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division":
                    if (scimUser.EnterpriseUser != null)
                    {
                        scimUser.EnterpriseUser.Division = (string)jObject["value"];

                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserEnterpriseUser = new ScimUserEnterpriseUser { };
                        scimUserEnterpriseUser.Department = (string)jObject["value"];
                        scimUserEnterpriseUser.ScimUser = scimUser;
                        scimUser.EnterpriseUser = scimUserEnterpriseUser;
                        if (!scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                            scimUser.Schemas = scimUser.Schemas.ToString() + ",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

                        _scimDataContext.ScimUserEnterpriseUsers.Add(scimUserEnterpriseUser);
                        _scimDataContext.ScimUsers.Update(scimUser);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department":
                    if (scimUser.EnterpriseUser != null)
                    {
                        scimUser.EnterpriseUser.Department = (string)jObject["value"];

                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var scimUserEnterpriseUser = new ScimUserEnterpriseUser { };
                        scimUserEnterpriseUser.Department = (string)jObject["value"];
                        scimUserEnterpriseUser.ScimUser = scimUser;
                        scimUser.EnterpriseUser = scimUserEnterpriseUser;
                        if (!scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                            scimUser.Schemas = scimUser.Schemas.ToString() + ",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

                        _scimDataContext.ScimUserEnterpriseUsers.Add(scimUserEnterpriseUser);
                        _scimDataContext.ScimUsers.Update(scimUser);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager":
                    if (scimUser.EnterpriseUser != null)
                    {
                        if (scimUser.EnterpriseUser.Manager != null)
                        {
                            var strmanager = (string)jObject["value"];
                            if (strmanager == null)
                            {
                                var manager = (JObject)jObject["value"];
                                scimUser.EnterpriseUser.Manager.Value = (string)manager["value"];
                                scimUser.EnterpriseUser.Manager.Ref = (string)manager["$ref"];
                                scimUser.EnterpriseUser.Manager.DisplayName = (string)manager["displayName"];
                            }
                            else
                            {
                                scimUser.EnterpriseUser.Manager.Value = strmanager;

                            }



                            _scimDataContext.ScimUserManagers.Update(scimUser.EnterpriseUser.Manager);
                            _scimDataContext.SaveChanges();
                        }
                        else
                        {
                            var scimUserManager = new ScimUserManager { };

                            var strmanager = (string)jObject["value"];
                            if (strmanager == null)
                            {
                                var manager = (JObject)jObject["value"];
                                scimUserManager.Value = (string)manager["value"];
                                scimUserManager.Ref = (string)manager["$ref"];
                                scimUserManager.DisplayName = (string)manager["displayName"];

                            }
                            else
                            {
                                scimUserManager.Value = strmanager;
                            }

                            var scimUserEnterpriseUser = scimUser.EnterpriseUser;
                            scimUserEnterpriseUser.Manager = scimUserManager;
                            scimUserManager.ScimUserEnterpriseUser = scimUserEnterpriseUser;

                            _scimDataContext.ScimUserManagers.Add(scimUserManager);
                            _scimDataContext.ScimUserEnterpriseUsers.Update(scimUserEnterpriseUser);
                            _scimDataContext.SaveChanges();
                        }
                    }
                    else
                    {
                        var scimUserManager = new ScimUserManager { };
                        var strmanager = (string)jObject["value"];
                        if(strmanager == null)
                        {
                            var manager = (JObject)jObject["value"];
                            scimUserManager.Value = (string)manager["value"];
                            scimUserManager.Ref = (string)manager["$ref"];
                            scimUserManager.DisplayName = (string)manager["displayName"];

                        }
                        else
                        {
                            scimUserManager.Value = strmanager;
                        }


                        var scimEnterpriseUser = new ScimUserEnterpriseUser { };
                        scimEnterpriseUser.Manager = scimUserManager;
                        scimUserManager.ScimUserEnterpriseUser = scimEnterpriseUser;
                        scimUser.EnterpriseUser = scimEnterpriseUser;


                        if (!scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                            scimUser.Schemas = scimUser.Schemas.ToString() + ",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

                        _scimDataContext.ScimUserManagers.Add(scimUserManager);
                        _scimDataContext.ScimUserEnterpriseUsers.Add(scimEnterpriseUser);
                        _scimDataContext.ScimUsers.Update(scimUser);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "externalId":
                    scimUser.ExternalId = (string)jObject["value"];
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;

                case "meta":
                    if(scimUser.Meta != null)
                    {
                        scimUser.Meta.LastModified = DateTime.UtcNow;
                        var version = Convert.ToBase64String(Encoding.UTF8.GetBytes(scimUser.Meta.LastModified.ToString()));
                        var etag = "W\\/\"" + version + "\"";
                        scimUser.Meta.Version = etag;
                        _scimDataContext.ScimUserMetas.Update(scimUser.Meta);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var creationTime = DateTime.UtcNow;

                        var version = Convert.ToBase64String(Encoding.UTF8.GetBytes(creationTime.ToString()));
                        var etag = "W\\/\"" + version + "\"";
                        var location = "https://" + _httpContextAccessor.HttpContext.Request.Host.Value + "/scim/v2/Users/" + scimUser.ScimUserId;

                        var scimUserMeta = new ScimUserMeta
                        {
                            ResourceType = "User",
                            Created = creationTime,
                            LastModified = creationTime,
                            Location = location,
                            Version = etag,
                            ScimUser = scimUser
                        };
                        
                        _scimDataContext.ScimUserMetas.Add(scimUserMeta);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                default:
                    throw new Exception();
            }

        }
        private void RemoveSpecificAttributeForScimUserByJson(ScimUser scimUser, JObject jObject)
        {

            if ((string)jObject["userName"] != null)
            {
                if (scimUser.UserName != null)
                {
                    scimUser.UserName = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((string)jObject["name"] != null)
            {
                if (scimUser.Name != null)
                {
                    var scimUserName = scimUser.Name;
                    _scimDataContext.ScimUserNames.Remove(scimUserName);
                    _scimDataContext.SaveChanges();
                }
            }
            if ((string)jObject["name.givenName"] != null)
            {
                if (scimUser.Name != null)
                {
                    var scimUserName = scimUser.Name;
                    scimUserName.GivenName = null;
                    _scimDataContext.ScimUserNames.Update(scimUserName);
                    _scimDataContext.SaveChanges();
                }
            }
            if ((string)jObject["name.familyName"] != null)
            {
                if (scimUser.Name != null)
                {
                    var scimUserName = scimUser.Name;
                    scimUserName.FamilyName = null;
                    _scimDataContext.ScimUserNames.Update(scimUserName);
                    _scimDataContext.SaveChanges();
                }
            }
            if ((string)jObject["name.formatted"] != null)
            {
                if (scimUser.Name != null)
                {
                    var scimUserName = scimUser.Name;
                    scimUserName.Formatted = null;
                    _scimDataContext.ScimUserNames.Update(scimUserName);
                    _scimDataContext.SaveChanges();
                }
            }
            if ((string)jObject["displayName"] != null)
            {
                if (scimUser.DisplayName != null)
                {
                    scimUser.DisplayName = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((string)jObject["nickName"] != null)
            {
                if (scimUser.NickName != null)
                {
                    scimUser.NickName = null;
                    _scimDataContext.ScimUsers.Update(scimUser);

                }
            }
            if ((string)jObject["profileUrl"] != null)
            {
                if (scimUser.ProfileUrl != null)
                {
                    scimUser.ProfileUrl = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((string)jObject["title"] != null)
            {
                if (scimUser.Title != null)
                {
                    scimUser.Title = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((string)jObject["userType"] != null)
            {
                if (scimUser.UserType != null)
                {
                    scimUser.UserType = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((string)jObject["preferredLanguage"] != null)
            {
                if (scimUser.PreferredLanguage != null)
                {
                    scimUser.PreferredLanguage = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((string)jObject["locale"] != null)
            {
                if (scimUser.Locale != null)
                {
                    scimUser.Locale = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((string)jObject["timezone"] != null)
            {
                if (scimUser.TimeZone != null)
                {
                    scimUser.TimeZone = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((string)jObject["active"] != null)
            {
                //throw new Exception();
            }
            if ((string)jObject["password"] != null)
            {
                if (scimUser.Password != null)
                {
                    scimUser.Password = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                }
            }
            if ((JArray)jObject["emails"] != null)
            {
                if (scimUser.Emails.Count > 0)
                {
                    var scimUserEmails = scimUser.Emails.ToList();
                    foreach (var scimUserEmail in scimUserEmails)
                    {
                        _scimDataContext.ScimUserEmails.Remove(scimUserEmail);
                    }
                }
            }
            if ((JArray)jObject["phoneNumbers"] != null)
            {
                if (scimUser.PhoneNumbers.Count > 0)
                {
                    var scimUserPhonenumbers = scimUser.PhoneNumbers.ToList();
                    foreach (var scimUserPhonenumber in scimUserPhonenumbers)
                    {
                        _scimDataContext.ScimUserPhoneNumbers.Remove(scimUserPhonenumber);
                    }
                }
            }
            if ((JArray)jObject["ims"] != null)
            {
                if (scimUser.Ims.Count > 0)
                {
                    var scimUserIms = scimUser.Ims.ToList();
                    foreach (var scimUserIm in scimUserIms)
                    {
                        _scimDataContext.ScimUserIms.Remove(scimUserIm);
                    }
                }
            }
            if ((JArray)jObject["photos"] != null)
            {
                if (scimUser.Photos.Count > 0)
                {
                    var scimUserPhotos = scimUser.Photos.ToList();
                    foreach (var scimUserPhoto in scimUserPhotos)
                    {
                        _scimDataContext.ScimUserPhotos.Remove(scimUserPhoto);
                    }
                }
            }
            if ((JArray)jObject["addresses"] != null)
            {
                if (scimUser.Addresses.Count > 0)
                {
                    var scimUserAddresses = scimUser.Addresses.ToList();
                    foreach (var scimUserAddresse in scimUserAddresses)
                    {
                        _scimDataContext.ScimUserAddresses.Remove(scimUserAddresse);
                    }
                }
            }
            if ((JArray)jObject["groups"] != null)
            {
                if (scimUser.Groups.Count > 0)
                {
                    var scimUserGroups = scimUser.Groups.ToList();
                    foreach (var scimUserGroup in scimUserGroups)
                    {
                        _scimDataContext.ScimUserGroups.Remove(scimUserGroup);
                    }
                }
            }
            if ((JArray)jObject["entitlements"] != null)
            {
                if (scimUser.Entitlements.Count > 0)
                {
                    var scimUserEntitlements = scimUser.Entitlements.ToList();
                    foreach (var scimUserEntitlement in scimUserEntitlements)
                    {
                        _scimDataContext.ScimUserEntitlements.Remove(scimUserEntitlement);
                    }
                }
            }
            if ((JArray)jObject["roles"] != null)
            {
                if (scimUser.Roles.Count > 0)
                {
                    var scimUserRoles = scimUser.Roles.ToList();
                    foreach (var scimUserRole in scimUserRoles)
                    {
                        _scimDataContext.ScimUserRoles.Remove(scimUserRole);
                    }
                }
            }
            if ((JArray)jObject["x509Certificates"] != null)
            {
                if (scimUser.X509Certificates.Count > 0)
                {
                    var scimUserX509Certificates = scimUser.X509Certificates.ToList();
                    foreach (var scimUserX509Certificate in scimUserX509Certificates)
                    {
                        _scimDataContext.ScimUserX509Certificates.Remove(scimUserX509Certificate);
                    }
                }
            }
            if ((JArray)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"] != null)
            {
                if (scimUser.EnterpriseUser != null)
                {
                    if (scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                        scimUser.Schemas = scimUser.Schemas.Replace(",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User","");
                    _scimDataContext.ScimUsers.Update(scimUser);

                    _scimDataContext.ScimUserEnterpriseUsers.Remove(scimUser.EnterpriseUser);
                }
            }
            if ((string)jObject["externalId"] != null)
            {
                if (scimUser.ExternalId != null)
                {
                    scimUser.ExternalId = null;
                    _scimDataContext.ScimUsers.Update(scimUser);

                }
            }
            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber"] != null)
            {
                if (scimUser.EnterpriseUser.EmployeeNumber != null)
                {
                    scimUser.EnterpriseUser.EmployeeNumber = null;
                    _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);

                }
            }
            if ((string)jObject["urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department"] != null)
            {
                if (scimUser.EnterpriseUser.Department != null)
                {
                    scimUser.EnterpriseUser.Department = null;
                    _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);

                }
            }
            _scimDataContext.SaveChanges();
        }
        private void RemoveSpecificAttributeForScimUserByPath(ScimUser scimUser, string path)
        {
            switch (path)
            {
                case "schemas":
                    scimUser.Schemas = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "userName":
                    break;
                case "name":
                    if (scimUser.Name != null)
                    {
                        _scimDataContext.ScimUserNames.Remove(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "name.formatted":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.Formatted = null;
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    
                    break;
                case "name.familyName":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.FamilyName = null;
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                   
                    break;
                case "name.givenName":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.GivenName = null;
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                   
                    break;
                case "name.middleName":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.MiddleName = null;
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    
                    break;
                case "name.honorificPrefix":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.HonorificPrefix = null;
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    
                    break;
                case "name.honorificSuffix":
                    if (scimUser.Name != null)
                    {
                        scimUser.Name.HonorificPrefix = null;
                        _scimDataContext.ScimUserNames.Update(scimUser.Name);
                        _scimDataContext.SaveChanges();
                    }
                    
                    break;

                case "displayName":
                    scimUser.DisplayName = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "nickName":
                    scimUser.NickName = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "profileUrl":
                    scimUser.ProfileUrl = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "title":
                    scimUser.Title = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "userType":
                    scimUser.UserType = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "preferredLanguage":
                    scimUser.PreferredLanguage = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "locale":
                    scimUser.Locale = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "timezone":
                    scimUser.TimeZone = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;
                case "active":
                    break;
                case "password":
                    scimUser.Password = null;
                    _scimDataContext.ScimUsers.Update(scimUser);
                    _scimDataContext.SaveChanges();
                    break;

                case "emails":
                    foreach (var scimUserEmail in scimUser.Emails.ToList())
                    {

                        _scimDataContext.ScimUserEmails.Remove(scimUserEmail);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "emails[type eq \"work\"].value":
                    if (scimUser.Emails.Count > 0)
                    {
                        var scimUserWorkEmail = scimUser.Emails.Where(c => c.Type == "work").FirstOrDefault();
                        if (scimUserWorkEmail != null)
                        {
                            _scimDataContext.ScimUserEmails.Remove(scimUserWorkEmail);
                            _scimDataContext.SaveChanges();
                        }
                        
                    }
                    
                    break;

                case "phoneNumbers":
                    foreach (var scimUserPhoneNumber in scimUser.PhoneNumbers.ToList())
                    {
                        _scimDataContext.ScimUserPhoneNumbers.Remove(scimUserPhoneNumber);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "phoneNumbers[type eq \"work\"].value":
                    foreach (var scimUserPhoneNumber in scimUser.PhoneNumbers.ToList())
                    {
                        if(scimUserPhoneNumber.Type == "work")
                        {
                            if(scimUserPhoneNumber.Value != null)
                            {
                                scimUserPhoneNumber.Value = null;
                                _scimDataContext.ScimUserPhoneNumbers.Update(scimUserPhoneNumber);
                                _scimDataContext.SaveChanges();
                            }

                        }
                    }
                    break;
                case "phoneNumbers[type eq \"mobile\"].value":
                    foreach (var scimUserPhoneNumber in scimUser.PhoneNumbers.ToList())
                    {
                        if (scimUserPhoneNumber.Type == "mobile")
                        {
                            if (scimUserPhoneNumber.Value != null)
                            {
                                scimUserPhoneNumber.Value = null;
                                _scimDataContext.ScimUserPhoneNumbers.Update(scimUserPhoneNumber);
                                _scimDataContext.SaveChanges();
                            }

                        }
                    }
                    break;
                case "phoneNumbers[type eq \"fax\"].value":
                    foreach (var scimUserPhoneNumber in scimUser.PhoneNumbers.ToList())
                    {
                        if (scimUserPhoneNumber.Type == "fax")
                        {
                            if (scimUserPhoneNumber.Value != null)
                            {
                                scimUserPhoneNumber.Value = null;
                                _scimDataContext.ScimUserPhoneNumbers.Update(scimUserPhoneNumber);
                                _scimDataContext.SaveChanges();
                            }

                        }
                    }
                    break;
                case "ims":
                    foreach (var scimUserIm in scimUser.Ims.ToList())
                    {
                        _scimDataContext.ScimUserIms.Remove(scimUserIm);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "photos":
                    foreach (var scimUserPhoto in scimUser.Photos.ToList())
                    {
                        _scimDataContext.ScimUserPhotos.Remove(scimUserPhoto);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "addresses":
                    foreach (var scimUserAddress in scimUser.Addresses.ToList())
                    {
                        _scimDataContext.ScimUserAddresses.Remove(scimUserAddress);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "addresses[type eq \"work\"].formatted":
                    foreach (var scimUserAddress in scimUser.Addresses.ToList())
                    {
                        if(scimUserAddress.Type == "work")
                        {
                            if (scimUserAddress.Formatted != null)
                            {
                                scimUserAddress.Formatted = null;
                                _scimDataContext.ScimUserAddresses.Update(scimUserAddress);
                                _scimDataContext.SaveChanges();
                            }

                        }
                    }
                    break;
                case "addresses[type eq \"work\"].streetAddress":
                    foreach (var scimUserAddress in scimUser.Addresses.ToList())
                    {
                        if (scimUserAddress.Type == "work")
                        {
                            if (scimUserAddress.StreetAddress != null)
                            {
                                scimUserAddress.StreetAddress = null;
                                _scimDataContext.ScimUserAddresses.Update(scimUserAddress);
                                _scimDataContext.SaveChanges();
                            }

                        }
                    }
                    break;
                case "addresses[type eq \"work\"].locality":
                    foreach (var scimUserAddress in scimUser.Addresses.ToList())
                    {
                        if (scimUserAddress.Type == "work")
                        {
                            if (scimUserAddress.Locality != null)
                            {
                                scimUserAddress.Locality = null;
                                _scimDataContext.ScimUserAddresses.Update(scimUserAddress);
                                _scimDataContext.SaveChanges();
                            }
                        }

                    }
                    break;
                case "addresses[type eq \"work\"].region":
                    foreach (var scimUserAddress in scimUser.Addresses.ToList())
                    {
                        if (scimUserAddress.Type == "work")
                        {
                            if (scimUserAddress.Region != null)
                            {
                                scimUserAddress.Region = null;
                                _scimDataContext.ScimUserAddresses.Update(scimUserAddress);
                                _scimDataContext.SaveChanges();
                            }
                        }
                    }
                    break;
                case "addresses[type eq \"work\"].postalCode":
                    foreach (var scimUserAddress in scimUser.Addresses.ToList())
                    {
                        if (scimUserAddress.Type == "work")
                        {
                            if (scimUserAddress.PostalCode != null)
                            {
                                scimUserAddress.PostalCode = null;
                                _scimDataContext.ScimUserAddresses.Update(scimUserAddress);
                                _scimDataContext.SaveChanges();
                            }
                        }

                    }
                    break;
                case "addresses[type eq \"work\"].country":
                    foreach (var scimUserAddress in scimUser.Addresses.ToList())
                    {
                        if (scimUserAddress.Type == "work")
                        {
                            if (scimUserAddress.Country != null)
                            {
                                scimUserAddress.Country = null;
                                _scimDataContext.ScimUserAddresses.Update(scimUserAddress);
                                _scimDataContext.SaveChanges();
                            }
                        }

                    }
                    break;
                case "groups":
                    foreach (var scimUserGroup in scimUser.Groups.ToList())
                    {
                        _scimDataContext.ScimUserGroups.Remove(scimUserGroup);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "entitlements":
                    foreach (var scimUserEntitlement in scimUser.Entitlements.ToList())
                    {
                        _scimDataContext.ScimUserEntitlements.Remove(scimUserEntitlement);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "roles":
                    foreach (var scimUserRole in scimUser.Roles.ToList())
                    {
                        _scimDataContext.ScimUserRoles.Remove(scimUserRole);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "x509Certificates":
                    foreach (var scimUserX509Certificate in scimUser.X509Certificates.ToList())
                    {
                        _scimDataContext.ScimUserX509Certificates.Remove(scimUserX509Certificate);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User":
                    if (scimUser.EnterpriseUser != null)
                    {
                        if (scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                            scimUser.Schemas = scimUser.Schemas.Replace(",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", "");
                        _scimDataContext.ScimUsers.Update(scimUser);

                        _scimDataContext.ScimUserEnterpriseUsers.Remove(scimUser.EnterpriseUser);
                        _scimDataContext.SaveChanges();
                    }
                    break;

                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:employeeNumber":
                    if (scimUser.EnterpriseUser != null)
                    {
                        scimUser.EnterpriseUser.EmployeeNumber = null;
                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);

                        if ((scimUser.EnterpriseUser.EmployeeNumber == string.Empty)
                            && (scimUser.EnterpriseUser.CostCenter == string.Empty)
                            && (scimUser.EnterpriseUser.Organization == string.Empty)
                            && (scimUser.EnterpriseUser.Division == string.Empty)
                            && (scimUser.EnterpriseUser.Department == string.Empty)
                            && (scimUser.EnterpriseUser.Manager == null))
                        {
                            if (scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                                scimUser.Schemas = scimUser.Schemas.Replace(",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", "");
                            _scimDataContext.ScimUsers.Update(scimUser);
                        }
                            
                        _scimDataContext.SaveChanges();
                    }
                    
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:costCenter":
                    if (scimUser.EnterpriseUser != null)
                    {
                       
                        scimUser.EnterpriseUser.CostCenter = null;
                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);

                        if ((scimUser.EnterpriseUser.EmployeeNumber == string.Empty)
                            && (scimUser.EnterpriseUser.CostCenter == string.Empty)
                            && (scimUser.EnterpriseUser.Organization == string.Empty)
                            && (scimUser.EnterpriseUser.Division == string.Empty)
                            && (scimUser.EnterpriseUser.Department == string.Empty)
                            && (scimUser.EnterpriseUser.Manager == null))
                        {
                            if (scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                                scimUser.Schemas = scimUser.Schemas.Replace(",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", "");
                            _scimDataContext.ScimUsers.Update(scimUser);
                        }
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:organization":
                    if (scimUser.EnterpriseUser != null)
                    {
                        
                        scimUser.EnterpriseUser.Organization = null;
                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);

                        if ((scimUser.EnterpriseUser.EmployeeNumber == string.Empty)
                            && (scimUser.EnterpriseUser.CostCenter == string.Empty)
                            && (scimUser.EnterpriseUser.Organization == string.Empty)
                            && (scimUser.EnterpriseUser.Division == string.Empty)
                            && (scimUser.EnterpriseUser.Department == string.Empty)
                            && (scimUser.EnterpriseUser.Manager == null))
                        {
                            if (scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                                scimUser.Schemas = scimUser.Schemas.Replace(",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", "");
                            _scimDataContext.ScimUsers.Update(scimUser);
                        }
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:division":
                    if (scimUser.EnterpriseUser != null)
                    {
                       
                        scimUser.EnterpriseUser.Division = null;
                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);

                        if ((scimUser.EnterpriseUser.EmployeeNumber == string.Empty)
                            && (scimUser.EnterpriseUser.CostCenter == string.Empty)
                            && (scimUser.EnterpriseUser.Organization == string.Empty)
                            && (scimUser.EnterpriseUser.Division == string.Empty)
                            && (scimUser.EnterpriseUser.Department == string.Empty)
                            && (scimUser.EnterpriseUser.Manager == null))
                        {
                            if (scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                                scimUser.Schemas = scimUser.Schemas.Replace(",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", "");
                            _scimDataContext.ScimUsers.Update(scimUser);
                        }
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department":
                    if (scimUser.EnterpriseUser != null)
                    {
                        scimUser.EnterpriseUser.Department = null;
                        _scimDataContext.ScimUserEnterpriseUsers.Update(scimUser.EnterpriseUser);
                        if ((scimUser.EnterpriseUser.EmployeeNumber == string.Empty)
                            && (scimUser.EnterpriseUser.CostCenter == string.Empty)
                            && (scimUser.EnterpriseUser.Organization == string.Empty)
                            && (scimUser.EnterpriseUser.Division == string.Empty)
                            && (scimUser.EnterpriseUser.Department == string.Empty)
                            && (scimUser.EnterpriseUser.Manager == null))
                        {
                            if (scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                                scimUser.Schemas = scimUser.Schemas.Replace(",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", "");
                            _scimDataContext.ScimUsers.Update(scimUser);
                        }
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:manager":
                    if (scimUser.EnterpriseUser != null)
                    {
                        if (scimUser.EnterpriseUser.Manager != null)
                        {
                            _scimDataContext.ScimUserManagers.Remove(scimUser.EnterpriseUser.Manager);
                            if ((scimUser.EnterpriseUser.EmployeeNumber == string.Empty)
                                && (scimUser.EnterpriseUser.CostCenter == string.Empty)
                                && (scimUser.EnterpriseUser.Organization == string.Empty)
                                && (scimUser.EnterpriseUser.Division == string.Empty)
                                && (scimUser.EnterpriseUser.Department == string.Empty)
                                && (scimUser.EnterpriseUser.Manager == null))
                            {
                                if (scimUser.Schemas.Contains("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"))
                                    scimUser.Schemas = scimUser.Schemas.Replace(",urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", "");
                                _scimDataContext.ScimUsers.Update(scimUser);
                            }
                            _scimDataContext.SaveChanges();
                        }
                        
                    }
                    
                    break;

                case "externalId":
                    scimUser.ExternalId = null;
                    _scimDataContext.SaveChanges();
                    _scimDataContext.ScimUsers.Update(scimUser);
                    break;

                default:
                    throw new Exception();
            }

        }

        private JObject CreateReturnScimGroupJObject(ScimGroup scimGroup)
        {
            var jObject = new JObject() { };

            if(scimGroup.Schemas != null)
            {
                var jSchema = new JArray();
                var schemas = scimGroup.Schemas.Split(",");
                foreach (var schema in schemas)
                {
                    jSchema.Add(schema);
                }

                jObject.Add(new JProperty("schemas", jSchema));
            }

            if (scimGroup.ScimGroupId != null)
                jObject.Add(new JProperty("id", scimGroup.ScimGroupId));
            
            if (scimGroup.ExternalId != null)
                jObject.Add(new JProperty("externalId", scimGroup.ExternalId));

            if (scimGroup.DisplayName != null)
                jObject.Add(new JProperty("displayName", scimGroup.DisplayName));

            if(scimGroup.Members != null)
            {
                if (scimGroup.Members.Count() > 0)
                {
                    var jMembers = new JArray();
                    foreach (var scimGroupMember in scimGroup.Members)
                    {
                        var jMember = new JObject();

                        if (scimGroupMember.Value != null)
                            jMember.Add(new JProperty("value", scimGroupMember.Value));

                        if (scimGroupMember.Ref != null)
                            jMember.Add(new JProperty("$ref", scimGroupMember.Ref));

                        if (scimGroupMember.Display != null)
                            jMember.Add(new JProperty("display", scimGroupMember.Display));


                        jMembers.Add(jMember);

                    }
                    jObject.Add(new JProperty("members", jMembers));
                }


            }

            if (scimGroup.Meta != null)
            {
                var jOMeta = new JObject();
                if (scimGroup.Meta.ResourceType != null)
                    jOMeta.Add(new JProperty("resourceType", scimGroup.Meta.ResourceType));

                if (scimGroup.Meta.Created != null)
                    jOMeta.Add(new JProperty("created", scimGroup.Meta.Created));

                if (scimGroup.Meta.LastModified != null)
                    jOMeta.Add(new JProperty("lastModified", scimGroup.Meta.LastModified));

                if (scimGroup.Meta.Version != null)
                    jOMeta.Add(new JProperty("version", scimGroup.Meta.Version));

                if (scimGroup.Meta.Location != null)
                    jOMeta.Add(new JProperty("location", scimGroup.Meta.Location));

                jObject.Add(new JProperty("meta", jOMeta));
            }

            return jObject;
        }
        private async Task<IList<ScimGroup>> FilterScimGroupsCoreAsync(string filter, string excludedAttributes = null)
        {
            var attribute = filter.Split(" ")[0];
            var operatorWord = filter.Split(" ")[1];
            var value = filter.Split(" ")[2].Replace("\"", "");
            IList<ScimGroup> scimGroups = new List<ScimGroup>();

            switch (operatorWord)
            {
                case "eq":
                    switch (attribute)
                    {
                        case "displayName":
                            if(excludedAttributes == "members")
                            {
                                scimGroups = await _scimDataContext.ScimGroups.Where(c => c.DisplayName == value)
                                    .Include(c => c.Meta)
                                    .ToListAsync();
                            }
                            else
                            {
                                scimGroups = await _scimDataContext.ScimGroups.Where(c => c.DisplayName == value)
                                    .Include(c => c.Members)
                                    .Include(c => c.Meta)
                                    .ToListAsync();
                            }

                            break;
                    }
                    break;

            }
            return scimGroups;
        }
        private async Task<ScimGroup> PutScimGroupCoreAsync(Guid id, JObject jObject)
        {
            var scimGroup = await _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == id)
                .Include(c => c.Members)
                .Include(c => c.Meta)
                .FirstOrDefaultAsync();

            RemoveSpecificAttributeForScimGroupByPath(scimGroup, "schemas");
            RemoveSpecificAttributeForScimGroupByPath(scimGroup, "externalId");
            RemoveSpecificAttributeForScimGroupByPath(scimGroup, "displayName");
            RemoveSpecificAttributeForScimGroupByPath(scimGroup, "members");


            AddOrUpdateAttributesForScimGroupByJson(scimGroup, jObject);
            AddOrUpdateAttributeForScimGroupByPath(scimGroup, jObject, "meta");

            var putScimGroup = await _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == id)
                .Include(c => c.Members)
                .Include(c => c.Meta)
                .FirstOrDefaultAsync();

            return putScimGroup;
        }
        private async Task<ScimGroup> PatchScimGroupCoreAync(Guid id, JObject jObject)
        {

            if ((JArray)jObject["schemas"] == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };

            if ((JArray)jObject["Operations"] == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };
            var operationWord = (string)jObject["schemas"][0];
            if (operationWord != "urn:ietf:params:scim:api:messages:2.0:PatchOp")
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.BadRequest, ScimType = "invalidValue" };

            var scimGroup = await _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == id)
                .Include(c => c.Members)
                .Include(c => c.Meta)
                .FirstOrDefaultAsync();

            if (scimGroup == null)
                throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.NotFound };

            var operations = (JArray)jObject["Operations"];
            foreach (var operationJObject in operations)
            {
                string op = null;
                string path = null;

                if ((string)operationJObject["op"] != null)
                    op = (string)operationJObject["op"];
                if ((string)operationJObject["path"] != null)
                    path = (string)operationJObject["path"];

                if (op == "add" || op == "replace" || op == "Add" || op == "Replace")
                {
                    if (path == null)
                    {
                        if (op == "replace" || op == "Replace")
                            RemoveSpecificAttributeForScimGroupByJson(scimGroup, (JObject)operationJObject["value"]);

                        AddOrUpdateAttributesForScimGroupByJson(scimGroup, (JObject)operationJObject["value"]);
                    }
                    else
                    {
                        if (op == "replace" || op == "Replace")
                            RemoveSpecificAttributeForScimGroupByPath(scimGroup, path);

                        AddOrUpdateAttributeForScimGroupByPath(scimGroup, (JObject)operationJObject, path);
                    }
                }
                else if (op == "remove" || op == "Remove")
                {
                    RemoveSpecificAttributeForScimGroupByPath(scimGroup, path);
                }
            }


            AddOrUpdateAttributeForScimGroupByPath(scimGroup, jObject, "meta");

            var patchedScimGroup = _scimDataContext.ScimGroups.Where(c => c.ScimGroupId == id)
                .Include(c => c.Members)
                .Include(c => c.Meta)
                .FirstOrDefault();

            return patchedScimGroup;
        }
        private void AddOrUpdateAttributesForScimGroupByJson(ScimGroup scimGroup, JObject jObject)
        {
            if ((JArray)jObject["schemas"] != null)
            {
                var schemaString = new StringBuilder();
                for (var i = 0; i < jObject["schemas"].Count(); i++)
                {
                    schemaString.Append((string)jObject["schemas"][i]);

                    if ((i + 1) != (int)jObject["schemas"].Count())
                        schemaString.Append(",");
                }
                scimGroup.Schemas = schemaString.ToString();
                _scimDataContext.ScimGroups.Update(scimGroup);
            }
            if ((string)jObject["externalId"] != null)
            {
                scimGroup.ExternalId = (string)jObject["externalId"];
                _scimDataContext.ScimGroups.Update(scimGroup);
            }

            if ((string)jObject["displayName"] != null)
            {
                scimGroup.DisplayName = (string)jObject["displayName"];
                _scimDataContext.ScimGroups.Update(scimGroup);
            }

            if ((JArray)jObject["members"] != null)
            {
                foreach (var member in (JArray)jObject["members"])
                {
                    var scimGroupMember = new ScimGroupMember();
                    if ((string)member["value"] != null)
                        scimGroupMember.Value = (string)member["value"];
                    if ((string)member["$ref"] != null)
                        scimGroupMember.Ref = (string)member["$ref"];
                    if ((string)member["type"] != null)
                        scimGroupMember.Type = (string)member["type"];
                    if ((string)member["display"] != null)
                        scimGroupMember.Display = (string)member["display"];

                    scimGroupMember.ScimGroup = scimGroup;
                    _scimDataContext.ScimGroupMembers.Add(scimGroupMember);
                }

            }
            _scimDataContext.SaveChanges();

        }
        private void AddOrUpdateAttributeForScimGroupByPath(ScimGroup scimGroup, JObject jObject, string path)
        {
            switch (path)
            {
                case "externalId":
                    scimGroup.ExternalId = (string)jObject["value"];
                    _scimDataContext.ScimGroups.Update(scimGroup);
                    _scimDataContext.SaveChanges();
                    break;
                case "displayName":
                    scimGroup.DisplayName = (string)jObject["value"];
                    _scimDataContext.ScimGroups.Update(scimGroup);
                    _scimDataContext.SaveChanges();
                    break;
                case "members":
                    var members = (JArray)jObject["value"];
                    foreach (var member in members)
                    {
                        var scimUserGroupMember = new ScimGroupMember() { };
                        if ((string)member["value"] != null)
                            scimUserGroupMember.Value = (string)member["value"];
                        if ((string)member["$ref"] != null)
                            scimUserGroupMember.Ref = (string)member["$ref"];
                        if ((string)member["type"] != null)
                            scimUserGroupMember.Type = (string)member["type"];
                        if ((string)member["display"] != null)
                            scimUserGroupMember.Display = (string)member["display"];

                        scimUserGroupMember.ScimGroup = scimGroup;
                        _scimDataContext.ScimGroupMembers.Add(scimUserGroupMember);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                case "meta":
                    if (scimGroup.Meta != null)
                    {
                        scimGroup.Meta.LastModified = DateTime.UtcNow;
                        var version = Convert.ToBase64String(Encoding.UTF8.GetBytes(scimGroup.Meta.LastModified.ToString()));
                        var etag = "W\\/\"" + version + "\"";
                        scimGroup.Meta.Version = etag;
                        _scimDataContext.ScimGroupMetas.Update(scimGroup.Meta);
                        _scimDataContext.SaveChanges();
                    }
                    else
                    {
                        var creationTime = DateTime.UtcNow;

                        var version = Convert.ToBase64String(Encoding.UTF8.GetBytes(creationTime.ToString()));
                        var etag = "W\\/\"" + version + "\"";
                        var location = "https://" + _httpContextAccessor.HttpContext.Request.Host.Value + "/scim/v2/Groups/" + scimGroup.ScimGroupId;

                        var scimUserMeta = new ScimGroupMeta
                        {
                            ResourceType = "Group",
                            Created = creationTime,
                            LastModified = creationTime,
                            Location = location,
                            Version = etag,
                            ScimGroup = scimGroup
                        };

                        _scimDataContext.ScimGroupMetas.Add(scimUserMeta);
                        _scimDataContext.SaveChanges();
                    }
                    break;
            }
        }
        private void RemoveSpecificAttributeForScimGroupByJson(ScimGroup scimGroup, JObject jObject)
        {
            if ((string)jObject["externalId"] != null)
            {
                if (scimGroup.ExternalId != null)
                {
                    scimGroup.ExternalId = null;
                    _scimDataContext.ScimGroups.Update(scimGroup);
                }
            }
            if ((string)jObject["displayName"] != null)
            {
                if (scimGroup.DisplayName != null)
                {
                    scimGroup.DisplayName = null;
                    _scimDataContext.ScimGroups.Update(scimGroup);
                }
            }
            if ((JArray)jObject["members"] != null)
            {
                if (scimGroup.Members.Count > 0)
                {
                    var scimGroupMembers = scimGroup.Members.ToList();
                    foreach (var scimGroupMember in scimGroupMembers)
                    {
                        _scimDataContext.ScimGroupMembers.Remove(scimGroupMember);
                    }
                }
            }
            _scimDataContext.SaveChanges();

        }
        private void RemoveSpecificAttributeForScimGroupByPath(ScimGroup scimGroup, string path)
        {
            switch (path)
            {
                case "externalId":
                    scimGroup.ExternalId = null;
                    _scimDataContext.ScimGroups.Update(scimGroup);
                    _scimDataContext.SaveChanges();
                    break;
                case "schemas":
                    scimGroup.Schemas = null;
                    _scimDataContext.ScimGroups.Update(scimGroup);
                    _scimDataContext.SaveChanges();
                    break;
                case "displayName":
                    scimGroup.DisplayName = null;
                    _scimDataContext.ScimGroups.Update(scimGroup);
                    _scimDataContext.SaveChanges();
                    break;
                case "members":
                    foreach (var scimGroupMember in scimGroup.Members.ToList())
                    {

                        _scimDataContext.ScimGroupMembers.Remove(scimGroupMember);
                        _scimDataContext.SaveChanges();
                    }
                    break;
                default:
                    if(path.Contains("members[value eq"))
                    {

                        var memberId = path.Replace("members[value eq \"", "").Replace("\"]", "");

                        foreach (var scimGroupMember in scimGroup.Members.ToList())
                        {
                            if(scimGroupMember.Value == memberId)
                            {
                                _scimDataContext.ScimGroupMembers.Remove(scimGroupMember);
                                _scimDataContext.SaveChanges();
                            }
                        }

                    }
                    else
                    {
                        throw new Exception();
                    }
                    break;
            }
        }

        #endregion

    }
}
