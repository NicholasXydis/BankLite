using BankLite.Application.Options;
using Microsoft.Extensions.Options;

namespace BankLite.API.Middleware
{
    public class CsrfProtectionMiddleware
    {
        private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete
        };

        private readonly HashSet<string> _allowedOrigins;
        private readonly IWebHostEnvironment _environment;
        private readonly RequestDelegate _next;

        public CsrfProtectionMiddleware(
            RequestDelegate next,
            IWebHostEnvironment environment,
            IOptions<AllowedOriginsSettings> allowedOrigins)
        {
            _next = next;
            _environment = environment;
            _allowedOrigins = BuildAllowedOrigins(allowedOrigins.Value, environment);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_environment.IsEnvironment("Testing") ||
                !UnsafeMethods.Contains(context.Request.Method) ||
                !context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            if (IsAllowedRequestOrigin(context))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "Invalid request origin." });
        }

        private bool IsAllowedRequestOrigin(HttpContext context)
        {
            string? requestOrigin = context.Request.Headers.Origin.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(requestOrigin))
            {
                string? referer = context.Request.Headers.Referer.FirstOrDefault();
                if (!Uri.TryCreate(referer, UriKind.Absolute, out Uri? refererUri))
                {
                    return false;
                }

                requestOrigin = refererUri.GetLeftPart(UriPartial.Authority);
            }

            return _allowedOrigins.Contains(requestOrigin);
        }

        private static HashSet<string> BuildAllowedOrigins(AllowedOriginsSettings settings,
            IWebHostEnvironment environment)
        {
            HashSet<string> allowedOrigins = new(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(settings.Frontend))
            {
                allowedOrigins.Add(settings.Frontend);
            }

            if (environment.IsDevelopment())
            {
                allowedOrigins.Add("http://127.0.0.1:5500");
                allowedOrigins.Add("https://localhost:3000");
            }

            return allowedOrigins;
        }
    }
}