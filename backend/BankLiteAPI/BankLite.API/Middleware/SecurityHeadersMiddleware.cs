using BankLite.Application.Options;
using Microsoft.Extensions.Options;

namespace BankLite.API.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly HashSet<string> _configuredOrigins;
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next, IOptions<AllowedOriginsSettings> allowedOrigins)
        {
            _next = next;
            _configuredOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(allowedOrigins.Value.Frontend))
            {
                _configuredOrigins.Add(allowedOrigins.Value.Frontend);
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            context.Response.Headers.Append("X-DNS-Prefetch-Control", "off");
            context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
            context.Response.Headers.Append("Content-Security-Policy", BuildContentSecurityPolicy(context));
            context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
            context.Response.Headers.Append("Cache-Control", "no-store");

            if (context.Request.IsHttps)
            {
                context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }

            await _next(context);
        }

        private string BuildContentSecurityPolicy(HttpContext context)
        {
            HashSet<string> connectSources = new(StringComparer.OrdinalIgnoreCase) { "'self'" };

            foreach (string origin in _configuredOrigins)
            {
                connectSources.Add(origin);
            }

            string webSocketScheme = context.Request.IsHttps ? "wss" : "ws";
            connectSources.Add($"{webSocketScheme}://{context.Request.Host}");

            return "default-src 'self'; " +
                   "script-src 'self'; " +
                   "style-src 'self'; " +
                   "img-src 'self' data:; " +
                   "font-src 'self'; " +
                   $"connect-src {string.Join(' ', connectSources)}; " +
                   "frame-ancestors 'none'";
        }
    }
}