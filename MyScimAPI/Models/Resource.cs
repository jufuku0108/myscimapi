using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MyScimAPI.Models
{
    public class PagingParameter
    {
        private readonly int _maxPageSize = 30;
        private int _pageSize = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = (value > _maxPageSize) ? _maxPageSize : value;
            }
        }
    }

    public class ScimErrorException : Exception
    {
        private ErrorTypes _errorType;
        private string _scimType;
        private string _detail;
        private string _status;


        public enum ErrorTypes { 
            BadRequest = 400,
            UnAuthorized = 401,
            Forbidden = 403,
            NotFound = 404,
            InternalServerError = 500
        }
          
        public string[] Schemas { get; set; } = new string[] { "urn:ietf:params:scim:api:messages:2.0:Error" };

        public ErrorTypes ErrorType
        {
            get
            {
                return _errorType;
            }
            set
            {
                _errorType = value;
                if (value == ErrorTypes.BadRequest)
                {
                    _status = "400";
                }
                else if (value == ErrorTypes.UnAuthorized)
                {
                    _detail = "Authorization failure. The authorization header is invalid or missing.";
                    _status = "401";

                }
                else if(value == ErrorTypes.Forbidden)
                {
                    _detail = "Operation is not permitted based on the supplied authorization.";
                    _status = "403";
                }
                else if (value == ErrorTypes.NotFound)
                {
                    _detail = "Object not found.";
                    _status = "404";
                }
                else if (value == ErrorTypes.InternalServerError)
                {
                    _detail = "Internal Server Error.";
                    _status = "500";

                }


            }
        }
        public string ScimType {
            get 
            {
                return _scimType;
            } 
            set 
            {
                _scimType = value;
                _detail = FormatExceptionString(value);
            } 
        }
        public string Status {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
            }
        }
        public string Detail {
            get
            {
                return _detail;
            }
            set
            {
                _detail = value;
            }
        }
        public ScimErrorException() : base() { }

        private static string FormatExceptionString(string scimType)
        {
            string result;
            switch (scimType)
            {
                case "invalidFilter":
                    result = "The specified filter syntax was invalid or the specified attribute and filter comparison combination is not supported.";
                    break;
                case "tooMany":
                    result = "The specified filter yields many more results than the server is willing to calculate or process.";
                    break;
                case "uniqueness":
                    result = "One or more of the attribute values are already in use or are reserved.";
                    break;
                case "mutability":
                    result = "The attempted modification is not compatible with the target attribute's mutability or current state.";
                    break;
                case "invalidSyntax":
                    result = "The request body message structure was invalid or did not conform to the request schema.";
                    break;
                case "invalidPath":
                    result = "The 'path' attribute was invalid or malformed.";
                    break;
                case "noTarget":
                    result = "The specified 'path' did not yield an attribute or attribute value that could be operated on.";
                    break;
                case "invalidValue":
                    result = "A required value was missing, or the value specified was not compatible with the operation or attribute type.";
                    break;
                case "invalidVers":
                    result = "The specified SCIM protocol version is not supported.";
                    break;
                case "sensitive":
                    result = "The specified request cannot be completed, due to the passing of sensitive information in a request URI.";
                    break;
                default:
                    result = "Other error.";
                    break;
            }
            return result;

        }
    } 

    /*
    public class ApplicationUser : IdentityUser
    {
        public virtual ScimUser ScimUser { get; set; }
    }
     */
    
    public class HttpObject
    {
        public int HttpObjectId { get; set; }
        public DateTime DateTime { get; set; }
        public string Type { get; set; }
        public string Method { get; set; }
        public int StatusCode { get; set; }
        public string IpAddress { get; set; }
        public string Url { get; set; }
        public string Headers { get; set; }
        public string Body { get; set; }

    }

    public class StatisticsData
    {
        public int StatisticsDataId { get; set; }
        public DateTime DateTime { get; set; }
        public int TotalAccessCount { get; set; }
        public int TotalUserCount { get; set; }
        public int TotalGroupCount { get; set; }
        public int TotalGetCount { get; set; }
        public int TotalPostCount { get; set; }
        public int TotalPatchCount { get; set; }
        public int TotalDeleteCount { get; set; }

    }

}
