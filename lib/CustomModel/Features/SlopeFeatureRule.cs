// <copyright file="SlopeFeatureRule.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System.Globalization;

namespace AstreaEngine.Features
{
    /// <summary>
    /// Configures slope tolerances and penalties based on user capabilities.
    /// </summary>
    public class SlopeFeatureRule : IProfileFeatureRule
    {
        /// <inheritdoc />
        public int Order => 20;

        /// <inheritdoc />
        public void Apply(UserProfile profile, CustomModelBuilder builder)
        {
            if (profile.AvoidSlopes)
            {
                var maxSlope = profile.MaxSlope ?? 8.0;
                // Moderate warning threshold = 60% of max slope (e.g. 4.8° when max is 8°)
                var moderateSlope = maxSlope * 0.6;

                if (maxSlope <= 8.0)
                {
                    // Strict slope avoidance (e.g. wheelchairs)
                    builder.AddPriorityRule($"average_slope > {maxSlope.ToString("0.#", CultureInfo.InvariantCulture)}", 0.0);
                    builder.AddPriorityRule($"average_slope > {moderateSlope.ToString("0.#", CultureInfo.InvariantCulture)}", 0.3);
                }
                else
                {
                    // Moderate slope penalty (e.g. crutches or custom limit)
                    builder.AddPriorityRule($"average_slope > {maxSlope.ToString("0.#", CultureInfo.InvariantCulture)}", 0.2);
                    builder.AddPriorityRule($"average_slope > {moderateSlope.ToString("0.#", CultureInfo.InvariantCulture)}", 0.5);
                }

                builder.AddSpeedMultiply("average_slope > 3", 0.8);
                builder.AddSpeedMultiply("average_slope > 5", 0.6);
            }
            else
            {
                // Standard walking slope speed adjustments
                builder.AddSpeedMultiply("average_slope > 8", 0.8);
                builder.AddSpeedMultiply("average_slope > 12", 0.6);
            }
        }
    }
}
