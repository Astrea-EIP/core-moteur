// <copyright file="CustomModelBuilder.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text.Json.Nodes;

namespace AstreaEngine
{
    /// <summary>
    /// Fluent builder for creating GraphHopper Custom Model JSON configurations.
    /// </summary>
    public class CustomModelBuilder
    {
        private readonly JsonArray _priorityRules = new();
        private readonly JsonArray _speedRules = new();
        private int _distanceInfluence = 70;

        public CustomModelBuilder AddPriorityRule(string condition, double multiplyBy)
        {
            var rule = new JsonObject
            {
                { "if", condition },
                { "multiply_by", multiplyBy.ToString("0.##", CultureInfo.InvariantCulture) }
            };
            _priorityRules.Add(rule);
            return this;
        }

        public CustomModelBuilder AddSpeedLimit(string condition, double limitKmH)
        {
            var rule = new JsonObject
            {
                { "if", condition },
                { "limit_to", limitKmH.ToString("0.#", CultureInfo.InvariantCulture) }
            };
            _speedRules.Add(rule);
            return this;
        }

        public CustomModelBuilder AddSpeedMultiply(string condition, double multiplyBy)
        {
            var rule = new JsonObject
            {
                { "if", condition },
                { "multiply_by", multiplyBy.ToString("0.##", CultureInfo.InvariantCulture) }
            };
            _speedRules.Add(rule);
            return this;
        }

        public CustomModelBuilder SetDistanceInfluence(int distanceInfluence)
        {
            _distanceInfluence = distanceInfluence;
            return this;
        }

        /// <summary>
        /// Builds the final GraphHopper custom model as a <see cref="JsonObject"/>.
        /// </summary>
        /// <returns>A JSON object ready to be attached to the route request under "custom_model".</returns>
        public JsonObject Build()
        {
            return new JsonObject
            {
                { "distance_influence", _distanceInfluence },
                { "priority", _priorityRules },
                { "speed", _speedRules }
            };
        }
    }
}
