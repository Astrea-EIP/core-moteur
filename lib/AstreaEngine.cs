using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AstreaEngine
{
    /// <summary>
    /// HTTP client for Astrea Engine.
    /// Handles GET/POST requests and JSON parsing.
    /// </summary>
    public static class HttpCalls
    {
        private static readonly HttpClient _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public static string StripScheme(string host)
        {
            foreach (var prefix in new[] { "https://", "http://" })
            {
                if (host.StartsWith(prefix))
                {
                    return host.Substring(prefix.Length);
                }
            }
            return host;
        }

        public static string JsonError(string message)
        {
            var error = new JsonObject { { "error", message } };
            return error.ToJsonString();
        }

        public static async Task<string> DoGetAsync(string host, string path, string? body = null)
        {
            try
            {
                // Ensure host has scheme
                if (!host.StartsWith("http://") && !host.StartsWith("https://"))
                {
                    host = $"http://{host}";
                }

                var url = $"{host}{path}";

                HttpResponseMessage response;

                if (string.IsNullOrEmpty(body))
                {
                    // GET request
                    response = await _client.GetAsync(url);
                }
                else
                {
                    // POST request
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    response = await _client.PostAsync(url, content);
                }

                if ((int)response.StatusCode != 200)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    return JsonError($"HTTP {(int)response.StatusCode}: {responseText}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                // Verify it's valid JSON
                try
                {
                    JsonNode.Parse(responseBody);
                    return responseBody;
                }
                catch
                {
                    // If not valid JSON, wrap in status object
                    var status = new JsonObject { { "status", responseBody } };
                    return status.ToJsonString();
                }
            }
            catch (HttpRequestException ex)
            {
                return JsonError($"HTTP error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return JsonError("Request timeout");
            }
            catch (Exception ex)
            {
                return JsonError($"Unexpected error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Routes implementation for GraphHopper API.
    /// </summary>
    public static class Routes
    {
        /// <summary>
        /// Call GraphHopper /route endpoint.
        /// </summary>
        public static async Task<string> RouteAsync(string host, List<string> points, string userJson)
        {
            if (points.Count < 2)
            {
                return HttpCalls.JsonError("at least 2 points are required");
            }

            JsonNode? user;
            try
            {
                user = JsonNode.Parse(userJson);
            }
            catch
            {
                return HttpCalls.JsonError("Invalid user JSON");
            }

            // Extract preferences with defaults
            var profile = user?["profile"]?.GetValue<string>() ?? "foot";
            var locale = user?["locale"]?.GetValue<string>() ?? "en";
            var chDisable = user?["ch_disable"]?.GetValue<bool>() ?? false;

            // Build query string
            var queryParams = new List<string>();
            foreach (var point in points)
            {
                queryParams.Add($"point={Uri.EscapeDataString(point)}");
            }

            queryParams.Add($"profile={Uri.EscapeDataString(profile)}");
            queryParams.Add($"locale={Uri.EscapeDataString(locale)}");

            if (chDisable || profile != "foot")
            {
                queryParams.Add("ch.disable=true");
            }

            var queryString = string.Join("&", queryParams);
            return await HttpCalls.DoGetAsync(host, $"/route?{queryString}");
        }

        /// <summary>
        /// Call GraphHopper /health endpoint.
        /// </summary>
        public static async Task<string> HealthAsync(string host)
        {
            return await HttpCalls.DoGetAsync(host, "/health");
        }
    }

    /// <summary>
    /// Main Astrea Engine wrapper - C# implementation.
    /// Mirrors the API from the original C++ library.
    /// </summary>
    public class AstreaEngine
    {
        /// <summary>
        /// Route request.
        /// Points: pipe-separated "lat,lon" pairs, e.g. "47.21,-1.55|47.22,-1.54"
        /// UserJson: JSON object describing the person, e.g. {"profile":"disabled","locale":"fr"}
        /// Returns a JSON string.
        /// </summary>
        public static async Task<string> AstreaRouteAsync(string host, string pointsCsv, string userJson)
        {
            // Parse CSV points
            var points = new List<string>();
            if (!string.IsNullOrEmpty(pointsCsv))
            {
                points = pointsCsv.Split('|')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
            }

            return await Routes.RouteAsync(host, points, userJson);
        }

        /// <summary>
        /// Health check.
        /// </summary>
        public static async Task<string> AstreaHealthAsync(string host)
        {
            return await Routes.HealthAsync(host ?? "");
        }

        // Synchronous wrappers for convenience (blocking calls)

        /// <summary>
        /// Synchronous version of AstreaRouteAsync.
        /// </summary>
        public static string AstreaRoute(string host, string pointsCsv, string userJson)
        {
            return AstreaRouteAsync(host, pointsCsv, userJson).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronous version of AstreaHealthAsync.
        /// </summary>
        public static string AstreaHealth(string host)
        {
            return AstreaHealthAsync(host).GetAwaiter().GetResult();
        }
    }
}
