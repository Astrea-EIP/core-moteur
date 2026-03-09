// <copyright file="AstreaEngine.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AstreaEngine
{
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
        /// <param name="host">The host to call the route endpoint on.</param>
        /// <param name="pointsCsv">Pipe-separated "lat,lon" pairs.</param>
        /// <param name="userJson">JSON string describing the user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        /// <param name="host">The host to check health for.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<string> AstreaHealthAsync(string host)
        {
            return await Routes.HealthAsync(host ?? string.Empty);
        }

        // Synchronous wrappers (blocking calls)

        /// <summary>
        /// Synchronous version of AstreaRouteAsync.
        /// </summary>
        /// <param name="host">The host to call the route endpoint on.</param>
        /// <param name="pointsCsv">Pipe-separated "lat,lon" pairs.</param>
        /// <param name="userJson">JSON string describing the user.</param>
        /// <returns>A JSON string representing the route.</returns>
        public static string AstreaRoute(string host, string pointsCsv, string userJson)
        {
            return AstreaRouteAsync(host, pointsCsv, userJson).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronous version of AstreaHealthAsync.
        /// </summary>
        /// <param name="host">The host to check health for.</param>
        /// <returns>A JSON string indicating health status.</returns>
        public static string AstreaHealth(string host)
        {
            return AstreaHealthAsync(host).GetAwaiter().GetResult();
        }
    }
}
