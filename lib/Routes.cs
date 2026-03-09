// <copyright file="Routes.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AstreaEngine
{
    /// <summary>
    /// Routes implementation for GraphHopper API.
    /// </summary>
    public static class Routes
    {
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

        public static async Task<string> HealthAsync(string host)
        {
            return await HttpCalls.DoGetAsync(host, "/health");
        }
    }
}
