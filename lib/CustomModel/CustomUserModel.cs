// <copyright file="CustomUserModel.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System.Text.Json.Nodes;

namespace AstreaEngine
{
    /// <summary>
    /// Translates user accessibility profiles into GraphHopper Custom Model configurations.
    /// </summary>
#pragma warning disable CA1708 // Identifiers should differ by more than case (backward compatibility)
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
            jsonBody["profile"] = "foot";
            jsonBody["ch.disable"] = true;
            jsonBody["lm.disable"] = true;
        }

        /// <summary>
        /// Backward-compatible alias for BuildCustomModel.
        /// </summary>
        /// <param name="jsonBody">The GraphHopper /route request JSON body.</param>
        /// <param name="user">The user profile JSON node.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Backward compatibility")]
        public static void buildCustomModel(JsonObject jsonBody, JsonNode user)
        {
            BuildCustomModel(jsonBody, user);
        }
    }
}
