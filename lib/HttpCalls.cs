// <copyright file="HttpCalls.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AstreaEngine
{
    public static class HttpCalls
    {
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
        };

        public static string StripScheme(string host)
        {
            foreach (var prefix in new[] { "https://", "http://" })
            {
                if (host.StartsWith(prefix, StringComparison.Ordinal))
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
                if (!host.StartsWith("http://", StringComparison.Ordinal) && !host.StartsWith("https://", StringComparison.Ordinal))
                {
                    host = $"http://{host}";
                }

                var url = $"{host}{path}";

                HttpResponseMessage response;

                if (string.IsNullOrEmpty(body))
                {
                    response = await Client.GetAsync(url);
                }
                else
                {
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    response = await Client.PostAsync(url, content);
                }

                if ((int)response.StatusCode != 200)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    return JsonError($"HTTP {(int)response.StatusCode}: {responseText}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                try
                {
                    JsonNode.Parse(responseBody);
                    return responseBody;
                }
                catch
                {
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
}
