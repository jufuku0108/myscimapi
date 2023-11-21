using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using MyScimAPI.Models;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MyScimAPI.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyScimAPI.Extensions
{
    public class RequestResponseHandler
    {
        private readonly RequestDelegate _next;

        public RequestResponseHandler()
        {

        }
        public RequestResponseHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IScimService scimService)
        {
           
            using (var scimDataContext = context.RequestServices.GetRequiredService<ScimDataContext>())
            {
                // for request logging.
                var request = context.Request;
                var path = request.Path;
                
                if(path.ToString().Contains("/scim/v2/"))
                {
                    var requestLog = await FormatHttpRequest(request);
                    scimDataContext.HttpObjects.Add(requestLog);
                    scimDataContext.SaveChanges();
                }

                // for response logging.
                var response = context.Response;
                var originalBodyStream = response.Body;
                using (var responseBody = new MemoryStream())
                {

                    try
                    {
                        response.Body = responseBody;
                        await _next(context);

                        if(response.StatusCode == 401)
                            throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.UnAuthorized };
                        if(response.StatusCode == 403)
                            throw new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.Forbidden };


                        var responseLog = await FormatHttpResponse(response);
                        if (path.ToString().Contains("/scim/v2/"))
                        {
                            scimDataContext.HttpObjects.Add(responseLog);
                            scimDataContext.SaveChanges();
                        }

                        await responseBody.CopyToAsync(originalBodyStream);


                    }
                    catch (Exception exception)
                    {

                        ScimErrorException scimException;
                        int statusCode;
                        var exceptionType = exception.GetType();
                        if (exceptionType == typeof(ScimErrorException))
                        {
                            scimException = exception as ScimErrorException;
                            statusCode = (int)scimException.ErrorType;
                        }
                        else
                        {
                            scimException = new ScimErrorException() { ErrorType = ScimErrorException.ErrorTypes.InternalServerError };
                            statusCode = 500;
                        }

                        var result = scimService.CreateJsonException(scimException);

                        context.Response.ContentType = "application/scim+json";
                        context.Response.StatusCode = statusCode;
                        using (var stream = GenerateStream(result.ToString()))
                        {
                            await stream.CopyToAsync(originalBodyStream);
                            stream.Position = 0;
                            await stream.CopyToAsync(context.Response.Body);

                        }
                        var responseLog = await FormatHttpResponse(context.Response);
                        scimDataContext.HttpObjects.Add(responseLog);
                        scimDataContext.SaveChanges();

                    }
                    
                     

                }
            }

        }

        
        private Stream GenerateStream(string result)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(result);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        

        private async Task<HttpObject> FormatHttpRequest(HttpRequest request)
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = UTF8Encoding.UTF8.GetString(buffer);
            if (bodyAsText.Length > 2000)
                bodyAsText = bodyAsText.Substring(0, 2000);
            request.Body.Position = 0;

            var requestHeaders = request.Headers.ToList();
            var requestHeaderBuilder = new StringBuilder();
            foreach (var requestHeader in requestHeaders)
            {
                requestHeaderBuilder.Append($"{requestHeader.Key}: {string.Join(",", requestHeader.Value)}   {Environment.NewLine}");
            }

            var requestObject = new HttpObject()
            {
                DateTime = DateTime.UtcNow,
                Type = "Request",
                Method = request.Method,
                Headers = requestHeaderBuilder.ToString(),
                IpAddress = request.HttpContext.Connection.RemoteIpAddress.ToString(),
                Url = $"{request.Scheme}://{request.Host}{request.Path}{WebUtility.UrlDecode(request.QueryString.ToString())}",
                Body = bodyAsText
            };
            return requestObject;

        }
        private async Task<HttpObject> FormatHttpResponse(HttpResponse response)
        {

            response.Body.Seek(0, SeekOrigin.Begin);
            var textBody = await new StreamReader(response.Body).ReadToEndAsync();
            if(textBody.Length > 2000)
                 textBody = textBody.Substring(0, 2000);
            response.Body.Seek(0, SeekOrigin.Begin);

            var responseHeaders = response.Headers.ToList();
            var responseHeaderBuilder = new StringBuilder();
            foreach (var responseHeader in responseHeaders)
            {
                responseHeaderBuilder.Append($"{responseHeader.Key}: {string.Join(",", responseHeader.Value)}   {Environment.NewLine}");
            }
            var responseObject = new HttpObject()
            {
                DateTime = DateTime.UtcNow,
                Type = "Response",
                StatusCode = response.StatusCode,
                Headers = responseHeaderBuilder.ToString(),
                IpAddress = response.HttpContext.Connection.LocalIpAddress.ToString(),
                Body = textBody,
            };
            return responseObject;
        }
    }
}
