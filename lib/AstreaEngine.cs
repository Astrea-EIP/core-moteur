// <copyright file="AstreaEngine.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AstreaEngine
{
    /// <summary>
    /// Represents a geographic point with latitude, longitude, and optional elevation.
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

        /// <summary>
        /// Gets or sets the elevation in meters, if available.
        /// </summary>
        [JsonPropertyName("elevation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Elevation { get; set; }
    }

    /// <summary>
    /// Represents a single navigation turn-by-turn instruction.
    /// </summary>
    public class InstructionResponse
    {
        /// <summary>
        /// Gets or sets the human-readable instruction text.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the street or path.
        /// </summary>
        [JsonPropertyName("street_name")]
        public string StreetName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the distance of this step in meters.
        /// </summary>
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        /// <summary>
        /// Gets or sets the estimated time for this step in milliseconds.
        /// </summary>
        [JsonPropertyName("time")]
        public long Time { get; set; }

        /// <summary>
        /// Gets or sets the turn direction indicator code.
        /// </summary>
        [JsonPropertyName("sign")]
        public int Sign { get; set; }

        /// <summary>
        /// Gets or sets the coordinate interval [startIndex, endIndex] for this step.
        /// </summary>
        [JsonPropertyName("interval")]
        public int[] Interval { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Gets or sets the initial heading in degrees at the start of the step.
        /// </summary>
        [JsonPropertyName("heading")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Heading { get; set; }

        /// <summary>
        /// Gets or sets the heading in degrees at the end of the step.
        /// </summary>
        [JsonPropertyName("last_heading")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? LastHeading { get; set; }
    }

    /// <summary>
    /// Represents the complete route response containing metrics, geometry, and instructions.
    /// </summary>
    public class RouteResponse
    {
        /// <summary>
        /// Gets or sets the total route distance in meters.
        /// </summary>
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        /// <summary>
        /// Gets or sets the total route duration in milliseconds.
        /// </summary>
        [JsonPropertyName("time")]
        public long Time { get; set; }

        /// <summary>
        /// Gets or sets the total ascend in meters.
        /// </summary>
        [JsonPropertyName("ascend")]
        public double Ascend { get; set; }

        /// <summary>
        /// Gets or sets the total descend in meters.
        /// </summary>
        [JsonPropertyName("descend")]
        public double Descend { get; set; }

        /// <summary>
        /// Gets or sets the bounding box [minLng, minLat, maxLng, maxLat].
        /// </summary>
        [JsonPropertyName("bbox")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double[]? BBox { get; set; }

        /// <summary>
        /// Gets or sets the list of geographic points along the route.
        /// </summary>
        [JsonPropertyName("points")]
        public List<PointResponse> Points { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of turn-by-turn navigation instructions.
        /// </summary>
        [JsonPropertyName("instructions")]
        public List<InstructionResponse> Instructions { get; set; } = new();
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
        /// UserJson: JSON object describing the person, e.g. {"profile":"wheelchair"}
        /// Locale: Language code for navigation instructions (defaults to "fr").
        /// Returns a RouteResponse object containing route metrics, points, and instructions.
        /// </summary>
        /// <param name="host">The host to call the route endpoint on.</param>
        /// <param name="points">List of geographic points with latitude and longitude.</param>
        /// <param name="userJson">JSON string describing the user.</param>
        /// <param name="locale">Language code for instructions (e.g. "fr", "en"). Defaults to "fr".</param>
        /// <returns>A <see cref="Task{RouteResponse}"/> representing the asynchronous operation.</returns>
        public static async Task<RouteResponse> AstreaRouteAsync(string host, List<PointResponse> points, string userJson, string locale = "fr")
        {
            // Convert PointResponse objects to string format expected by Routes.RouteAsync
            var pointStrings = points?.
                Select(p => $"{p.Lat.ToString(CultureInfo.InvariantCulture)},{p.Lng.ToString(CultureInfo.InvariantCulture)}")
                .ToList() ?? new List<string>();

            return await Routes.RouteAsync(host, pointStrings, userJson, locale);
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
        /// <param name="locale">Language code for instructions (e.g. "fr", "en"). Defaults to "fr".</param>
        /// <returns>A RouteResponse object representing the route.</returns>
        public static RouteResponse AstreaRoute(string host, List<PointResponse> points, string userJson, string locale = "fr")
        {
            return AstreaRouteAsync(host, points, userJson, locale).GetAwaiter().GetResult();
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
