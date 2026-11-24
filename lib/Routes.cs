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
        public static RouteResponse ParseJson(string json)
        {
            var result = new RouteResponse();

            try
            {
                var jsonNode = JsonNode.Parse(json);
                if (jsonNode?["paths"] is JsonArray paths && paths.Count > 0)
                {
                    var path = paths[0];
                    if (path != null)
                    {
                        result.Distance = path["distance"]?.GetValue<double>() ?? 0;
                        result.Time = path["time"]?.GetValue<long>() ?? 0;
                        result.Ascend = path["ascend"]?.GetValue<double>() ?? 0;
                        result.Descend = path["descend"]?.GetValue<double>() ?? 0;

                        if (path["bbox"] is JsonArray bboxArray)
                        {
                            var bbox = new List<double>();
                            foreach (var item in bboxArray)
                            {
                                if (item != null)
                                {
                                    bbox.Add(item.GetValue<double>());
                                }
                            }
                            result.BBox = bbox.ToArray();
                        }

                        if (path["points"]?["coordinates"] is JsonArray coordinates)
                        {
                            foreach (var coord in coordinates)
                            {
                                if (coord is JsonArray point && point.Count >= 2 && point[0] != null && point[1] != null)
                                {
                                    var pt = new PointResponse
                                    {
                                        Lat = point[1]!.GetValue<double>(),
                                        Lng = point[0]!.GetValue<double>(),
                                    };

                                    if (point.Count >= 3 && point[2] != null)
                                    {
                                        pt.Elevation = point[2]!.GetValue<double>();
                                    }

                                    result.Points.Add(pt);
                                }
                            }
                        }

                        if (path["instructions"] is JsonArray instructionsArray)
                        {
                            foreach (var instNode in instructionsArray)
                            {
                                if (instNode is JsonObject instObj)
                                {
                                    var instruction = new InstructionResponse
                                    {
                                        Text = instObj["text"]?.GetValue<string>() ?? string.Empty,
                                        StreetName = instObj["street_name"]?.GetValue<string>() ?? string.Empty,
                                        Distance = instObj["distance"]?.GetValue<double>() ?? 0,
                                        Time = instObj["time"]?.GetValue<long>() ?? 0,
                                        Sign = instObj["sign"]?.GetValue<int>() ?? 0,
                                    };

                                    if (instObj["interval"] is JsonArray intervalArr)
                                    {
                                        var intervals = new List<int>();
                                        foreach (var i in intervalArr)
                                        {
                                            if (i != null)
                                            {
                                                intervals.Add(i.GetValue<int>());
                                            }
                                        }
                                        instruction.Interval = intervals.ToArray();
                                    }

                                    if (instObj["heading"] != null)
                                    {
                                        instruction.Heading = instObj["heading"]!.GetValue<double>();
                                    }

                                    if (instObj["last_heading"] != null)
                                    {
                                        instruction.LastHeading = instObj["last_heading"]!.GetValue<double>();
                                    }

                                    result.Instructions.Add(instruction);
                                }
                            }
                        }

                        return result;
                    }
                }

                if (jsonNode?["error"] != null)
                {
                    throw new InvalidDataException($"API error: {jsonNode["error"]?.ToString()}");
                }
                if (jsonNode?["message"] != null)
                {
                    throw new InvalidDataException($"GraphHopper error: {jsonNode["message"]?.ToString()}");
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

        public static async Task<RouteResponse> RouteAsync(string host, List<string> points, string userJson, string locale = "fr")
        {
            if (points.Count < 2)
            {
                throw new ValidationException("At least 2 points are required for routing.");
            }

            JsonNode? user = null;
            if (!string.IsNullOrWhiteSpace(userJson))
            {
                try
                {
                    user = JsonNode.Parse(userJson);
                }
                catch (Exception ex)
                {
                    throw new ValidationException("Invalid user JSON.", ex);
                }
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
                else
                {
                    throw new ValidationException($"Invalid coordinate format: '{point}'. Expected 'lat,lng'.");
                }
            }

            if (pointsArray.Count < 2)
            {
                throw new ValidationException("At least 2 valid points are required for routing.");
            }

            var resolvedLocale = string.IsNullOrWhiteSpace(locale) ? "fr" : locale;

            var jsonBody = new JsonObject
            {
                { "points", pointsArray },
                { "locale", resolvedLocale },
                { "points_encoded", false },
            };

            // Construction of the user profile and custom model based on the user JSON.
            // When user is null, default to standard pedestrian routing ("foot") with Contraction Hierarchies enabled.
            // When user is provided, BuildCustomModel applies custom speed and priority rules on top of the "foot" base profile.
            if (user == null)
            {
                jsonBody["profile"] = "foot";
                jsonBody["ch.disable"] = false;
            }
            else
            {
                CustomUserModel.BuildCustomModel(jsonBody, user);
            }

            var responseJson = await HttpCalls.DoGetAsync(host, $"/route", jsonBody.ToJsonString());

            try
            {
                var jsonNode = JsonNode.Parse(responseJson);
                if (jsonNode?["error"] != null)
                {
                    throw new InvalidOperationException($"API error: {jsonNode["error"]?.ToString()}");
                }

                if (jsonNode?["message"] != null)
                {
                    throw new InvalidOperationException($"API error: {jsonNode["message"]?.ToString()}");
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

            return ParseJson(responseJson);
        }

        public static async Task<string> HealthAsync(string host)
        {
            return await HttpCalls.DoGetAsync(host, "/health");
        }
    }
}
