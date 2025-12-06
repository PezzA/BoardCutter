using System.Net;
using Microsoft.Extensions.Logging;

namespace BoardCutter.Web.Middleware
{
    public class SvelteDevProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _svelteDevServerUrl;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SvelteDevProxyMiddleware> _logger;

        public SvelteDevProxyMiddleware(RequestDelegate next, ILogger<SvelteDevProxyMiddleware> logger, string svelteDevServerUrl = "http://localhost:5173")
        {
            _next = next;
            _logger = logger;
            _svelteDevServerUrl = svelteDevServerUrl;
            _httpClient = new HttpClient();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only proxy in development and for non-API routes
            if (ShouldProxy(context))
            {
                _logger.LogInformation("Proxying request {Method} {Path} to Svelte dev server at {SvelteUrl}", 
                    context.Request.Method, 
                    context.Request.Path, 
                    _svelteDevServerUrl);
                await ProxyToSvelteDevServer(context);
                return;
            }

            await _next(context);
        }

        private bool ShouldProxy(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // Don't proxy Razor Page routes - let ASP.NET Core handle them
            if (path == "/" || path == "" ||
                path == "/claims" ||
                path == "/error" ||
                path == "/login" ||
                path == "/logout" ||
                path == "/privacy" ||
                path == "/callback" ||
                path == "/stratagems" ||
                path == "/twenty48")
            {
                return false;
            }
            
            // Don't proxy API routes, SignalR hubs, or static assets
            if (path.StartsWith("/api") || 
                path.StartsWith("/health") ||
                path.StartsWith("/twenty48hub") ||
                path.StartsWith("/gamelobbyhub") ||
                path.StartsWith("/_framework") ||
                path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/lib"))
            {
                return false;
            }

            return true;
        }

        private async Task ProxyToSvelteDevServer(HttpContext context)
        {
            try
            {
                var requestUri = $"{_svelteDevServerUrl}{context.Request.Path}{context.Request.QueryString}";
                _logger.LogDebug("Sending proxied request to: {RequestUri}", requestUri);
                
                using var requestMessage = new HttpRequestMessage(
                    new HttpMethod(context.Request.Method), 
                    requestUri);

                // Copy headers (except Host)
                foreach (var header in context.Request.Headers)
                {
                    if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                    {
                        requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // Copy body for POST/PUT requests
                if (context.Request.ContentLength > 0)
                {
                    requestMessage.Content = new StreamContent(context.Request.Body);
                    if (context.Request.ContentType != null)
                    {
                        requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
                    }
                }

                using var responseMessage = await _httpClient.SendAsync(requestMessage);
                
                _logger.LogDebug("Received response from Svelte dev server: {StatusCode} for {Path}", 
                    responseMessage.StatusCode, 
                    context.Request.Path);

                // Copy response status
                context.Response.StatusCode = (int)responseMessage.StatusCode;

                // Copy response headers (excluding HTTP/1.1 specific headers that are invalid for HTTP/2 and HTTP/3)
                foreach (var header in responseMessage.Headers)
                {
                    if (IsValidHeaderForHttpVersion(header.Key))
                    {
                        context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                    }
                }

                foreach (var header in responseMessage.Content.Headers)
                {
                    if (IsValidHeaderForHttpVersion(header.Key))
                    {
                        context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                    }
                }

                // Copy response body only if the status code allows it
                // HTTP 304 (Not Modified) and some other status codes should not have a body
                if (ShouldCopyResponseBody(responseMessage.StatusCode))
                {
                    await responseMessage.Content.CopyToAsync(context.Response.Body);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to proxy request {Method} {Path} to Svelte dev server at {SvelteUrl}", 
                    context.Request.Method, 
                    context.Request.Path, 
                    _svelteDevServerUrl);
                
                // If Svelte dev server is not running, fall back to serving static files
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Svelte dev server not available. Please run 'npm run dev' in the BoardCutter.Client folder.");
            }
        }

        private static bool ShouldCopyResponseBody(System.Net.HttpStatusCode statusCode)
        {
            // These status codes should not have a response body
            return statusCode != System.Net.HttpStatusCode.NotModified && // 304
                   statusCode != System.Net.HttpStatusCode.NoContent && // 204
                   statusCode != System.Net.HttpStatusCode.ResetContent && // 205
                   (int)statusCode != 100 && // Continue
                   (int)statusCode != 101 && // Switching Protocols
                   (int)statusCode != 102 && // Processing
                   (int)statusCode != 103;   // Early Hints
        }

        private static bool IsValidHeaderForHttpVersion(string headerName)
        {
            // Filter out HTTP/1.1 specific headers that are invalid for HTTP/2 and HTTP/3
            // Add any other headers that need to be filtered out based on your requirements
            return !headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase) &&
                   !headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) &&
                   !headerName.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase) &&
                   !headerName.Equals("Upgrade", StringComparison.OrdinalIgnoreCase) &&
                   !headerName.Equals("Proxy-Connection", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
