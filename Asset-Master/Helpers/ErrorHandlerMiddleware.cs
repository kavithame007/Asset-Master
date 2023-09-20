using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace Asset_Master.Helpers;

// You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {

            await _next(httpContext);
        }
        catch (Exception ex)
        {
            var response = httpContext.Response;
            response.ContentType = "application/json";

            switch (ex)
            {
                case ApplicationException e:
                    // custom application error
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case KeyNotFoundException e:
                    // not found error
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                default:
                    // unhandled error
                    _logger.LogError(ex, ex.Message);
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var result = JsonSerializer.Serialize(new { message = ex?.Message });
            await response.WriteAsync(result);
        }
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class ErrorHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandlerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlerMiddleware>();
    }
}
