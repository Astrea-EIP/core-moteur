// <copyright file="AstreaEngine.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AstreaEngine
{
    /// <summary>
    /// Represents a geographic point with latitude and longitude.
    /// </summary>
    public class PointResponse
    {
        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    /// <summary>
    /// Main Astrea Engine wrapper - C# implementation.
    /// Mirrors the API from the original C++ library.
    /// </summary>
    public class AstreaEngine
    {
        /// <summary>
        /// Route request.
        /// Points: List of PointResponse objects with lat and lng coordinates.
        /// UserJson: JSON object describing the person, e.g. {"profile":"disabled","locale":"fr"}
        /// Returns a list of PointResponse objects.
        /// </summary>
        /// <param name="host">The host to call the route endpoint on.</param>
        /// <param name="points">List of geographic points with latitude and longitude.</param>
        /// <param name="userJson">JSON string describing the user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<List<PointResponse>> AstreaRouteAsync(string host, List<PointResponse> points, string userJson)
        {
            // Convert PointResponse objects to string format expected by Routes.RouteAsync
            var pointStrings = points?.
                Select(p => $"{p.Lat.ToString(CultureInfo.InvariantCulture)},{p.Lng.ToString(CultureInfo.InvariantCulture)}")
                .ToList() ?? new List<string>();

            return await Routes.RouteAsync(host, pointStrings, userJson);
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
        /// <param name="points">List of geographic points with latitude and longitude.</param>
        /// <param name="userJson">JSON string describing the user.</param>
        /// <returns>A list of PointResponse objects representing the route.</returns>
        public static List<PointResponse> AstreaRoute(string host, List<PointResponse> points, string userJson)
        {
            return AstreaRouteAsync(host, points, userJson).GetAwaiter().GetResult();
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
