// <copyright file="CustomUserModel.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System.Text.Json.Nodes;

namespace AstreaEngine
{
    /// <summary>
    /// Translates user accessibility profiles into GraphHopper Custom Model configurations.
    /// </summary>
    public static class CustomUserModel
    {
        /// <summary>
        /// Builds the custom model and attaches it to the GraphHopper request payload.
        /// </summary>
        /// <param name="jsonBody">The GraphHopper /route request JSON body.</param>
        /// <param name="user">The user profile JSON node.</param>
        public static void BuildCustomModel(JsonObject jsonBody, JsonNode user)
        {
            var profile = UserProfile.Parse(user);
            var customModel = ProfileFeatureRegistry.Default.BuildCustomModel(profile);

            jsonBody["custom_model"] = customModel;

            // GraphHopper custom models are evaluated dynamically against the base pedestrian network ("foot").
            // Specific accessibility profiles (wheelchair, blind, crutches, etc.) are expressed as speed and priority
            // rules modifying the "foot" graph. Contraction Hierarchies (CH) and Landmark (LM) are disabled to evaluate
            // custom model rules dynamically per request.
            jsonBody["profile"] = "foot";
            jsonBody["ch.disable"] = true;
            jsonBody["lm.disable"] = true;
        }
    }
}
