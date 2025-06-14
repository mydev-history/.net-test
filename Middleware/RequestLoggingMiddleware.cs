using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CurrencyConverterApi.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var correlationId = Guid.NewGuid().ToString();  // Generate Correlation ID for each request

            // Capture client IP
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();

            // Capture the ClientId from the JWT token (Check for ClaimTypes.Name or the ClientId claim you used)
            var clientId = httpContext.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown"; // Assuming ClientId is stored under ClaimTypes.Name

            // Start a stopwatch to measure the response time
            var stopwatch = Stopwatch.StartNew();

            // Log incoming request with structured logging
            _logger.LogInformation("Incoming request: {Method} {Path} from {ClientIp} (ClientId: {ClientId})", 
                httpContext.Request.Method, 
                httpContext.Request.Path, 
                clientIp, 
                clientId);

            // Add Correlation ID to the response headers so it can be traced later
            httpContext.Response.Headers.Add("X-Correlation-ID", correlationId);

            // Call the next middleware in the pipeline
            await _next(httpContext);

            // Stop the stopwatch once the response is processed
            stopwatch.Stop();

            // Log response details with structured logging
            _logger.LogInformation("Response: {StatusCode} {Method} {Path} took {ElapsedMilliseconds}ms from {ClientIp} (ClientId: {ClientId})", 
                httpContext.Response.StatusCode, 
                httpContext.Request.Method, 
                httpContext.Request.Path, 
                stopwatch.ElapsedMilliseconds, 
                clientIp, 
                clientId);
        }
    }
}
