using System.Net;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace VehicleServiceBooking.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
           
            if (context.Response.HasStarted)
            {
                throw; 
            }
            
            if (ex is UnauthorizedAccessException || 
                ex is SecurityTokenException ||
                ex is SecurityTokenValidationException ||
                ex is SecurityTokenExpiredException ||
                ex is SecurityTokenInvalidSignatureException)
            {
                throw; 
            }
            
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // Handle different exception types with appropriate status codes..
        int statusCode;
        string message;
        
        if (exception is UnauthorizedAccessException)
        {
            statusCode = (int)HttpStatusCode.Unauthorized;
            message = "Unauthorized access.";
        }
        else if (exception is ArgumentException || exception is InvalidOperationException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            message = exception.Message;
        }
        else
        {
            statusCode = (int)HttpStatusCode.InternalServerError;
            message = "An error occurred while processing your request.";
        }

        context.Response.StatusCode = statusCode;

        var response = new
        {
            error = new
            {
                message = message,
                details = statusCode == (int)HttpStatusCode.InternalServerError 
                    ? (context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true ? exception.Message : null)
                    : exception.Message,
                stackTrace = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
                    ? exception.StackTrace
                    : null
            }
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }
}


