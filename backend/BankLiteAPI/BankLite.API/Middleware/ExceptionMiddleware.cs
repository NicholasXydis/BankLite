using BankLite.Application.Exceptions;
using System.Text.Json;

namespace BankLite.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                context.Response.ContentType = "application/json";

                context.Response.StatusCode = ex switch
                {
                    BadRequestException => 400,
                    UnauthorizedAppException => 401,
                    InvalidOperationException => 400,
                    UnauthorizedAccessException => 401,
                    KeyNotFoundException => 404,
                    HttpRequestException => 502,
                    _ => 500
                };

                string message = context.Response.StatusCode >= 500
                    ? "An unexpected error occurred."
                    : ex.Message;
                var error = new { message };

                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            }
        }
    }
}