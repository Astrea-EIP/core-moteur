// <copyright file="Routes.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AstreaEngine
{
    /// <summary>
    /// Routes implementation for GraphHopper API.
    /// </summary>
    public static class Routes
    {
        private static List<PointResponse> parseJson(string json)
        {
            var result = new List<PointResponse>();

            try
            {
                var jsonNode = JsonNode.Parse(json);
                if (jsonNode?["paths"] is JsonArray paths && paths.Count > 0)
                {
                    var path = paths[0];
                    if (path?["points"]?["coordinates"] is JsonArray coordinates)
                    {
                        foreach (var coord in coordinates)
                        {
                            if (coord is JsonArray point && point.Count >= 2 && point[0] != null && point[1] != null)
                            {
                                result.Add(new PointResponse
                                {
                                    Lat = point[1]!.GetValue<double>(),
                                    Lng = point[0]!.GetValue<double>(),
                                });
                            }
                        }
                        return result;
                    }
                }
                throw new InvalidDataException("JSON response does not contain expected route structure.");
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Failed to parse route response JSON.", ex);
            }
        }

        public static async Task<List<PointResponse>> RouteAsync(string host, List<string> points, string userJson)
        {
            if (points.Count < 2)
            {
                throw new ValidationException("At least 2 points are required for routing.");
            }

            JsonNode? user;
            try
            {
                user = JsonNode.Parse(userJson);
            }
            catch
            {
                throw new ValidationException("Invalid user JSON.");
            }

            // Build the points array for the request
            var pointsArray = new JsonArray();
            foreach (var point in points)
            {
                var coords = point.Split(',');
                if (coords.Length == 2 && double.TryParse(coords[0],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var lat)
                    && double.TryParse(coords[1],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var lng))
                {
                    pointsArray.Add(new JsonArray { lng, lat });
                }
            }

            var jsonBody = new JsonObject
            {
                { "points", pointsArray },
                { "locale", "fr" }, // ! Hardcoded value, should be in the user JSON
                { "points_encoded", false },
            };

            // Construction of the user profile and custom model based on the user JSON
            // If user is null, use default profile (no accessibility features)
            // If user is not null, build a custom model based on the user profile and preferences
            if (user == null)
            {
                jsonBody["profile"] = "foot";
                jsonBody["ch.disable"] = "false";
            }
            else
            {
                jsonBody["ch.disable"] = "true";
                CustomUserModel.buildCustomModel(jsonBody, user);
            }

            var responseJson = await HttpCalls.DoGetAsync(host, $"/route", jsonBody.ToJsonString());

            try
            {
                var jsonNode = JsonNode.Parse(responseJson);
                if (jsonNode?["error"] != null)
                {
                    throw new InvalidOperationException($"API error: {jsonNode["error"]?.ToString()}");
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                // Not valid JSON or no error field, continue to parseJson
            }

            return parseJson(responseJson);
        }

        public static async Task<string> HealthAsync(string host)
        {
            return await HttpCalls.DoGetAsync(host, "/health");
        }
    }
}
