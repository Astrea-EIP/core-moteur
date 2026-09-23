// <copyright file="CustomUserModel.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System.Text.Json.Nodes;

namespace AstreaEngine
{
    public static class CustomUserModel
    {
        public static void buildCustomModel(JsonObject jsonBody, JsonNode user)
        {
            // TODO: Implement custom model building based on user profile and preferences.
            var speed = new JsonArray
            {
            };

            var priority = new JsonArray
            {
            };

            // TODO: create the factory that take those two arrays and populate with the right data

            var customModel = new JsonObject
            {
                { "speed", speed },
                { "priority", priority },
                { "distance_influence", 100 } // temp value
            };
            // jsonBody["custom_model"] = customModel;
            jsonBody["profile"] = "wheelchair"; // temp value
        }
    }
}
