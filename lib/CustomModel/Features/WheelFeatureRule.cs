// <copyright file="WheelFeatureRule.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

namespace AstreaEngine.Features
{
    /// <summary>
    /// Configures road surface and smoothness requirements for wheeled users
    /// (wheelchairs, strollers, luggage, carts, etc.).
    /// </summary>
    public class WheelFeatureRule : IProfileFeatureRule
    {
        /// <inheritdoc />
        public int Order => 30;

        /// <inheritdoc />
        public void Apply(UserProfile profile, CustomModelBuilder builder)
        {
            if (profile.HasWheels)
            {
                // Impassable surfaces & smoothness
                builder.AddPriorityRule("smoothness == IMPASSABLE", 0.0);
                builder.AddPriorityRule("surface == SAND || surface == OTHER", 0.0);

                // High penalty for cobblestones and rough ground
                builder.AddPriorityRule("surface == COBBLESTONE", 0.3);
                builder.AddPriorityRule(
                    "surface == GRAVEL || surface == FINE_GRAVEL || surface == UNPAVED || surface == COMPACTED || surface == DIRT || surface == GROUND",
                    0.5);

                // Smoothness penalties
                builder.AddPriorityRule("smoothness == INTERMEDIATE", 0.9);
                builder.AddPriorityRule("smoothness == BAD", 0.5);
                builder.AddPriorityRule(
                    "smoothness == VERY_BAD || smoothness == HORRIBLE || smoothness == VERY_HORRIBLE",
                    0.05);

                // Preferred safe ways for wheeled navigation
                builder.AddPriorityRule("footway == SIDEWALK", 1.3);
                builder.AddPriorityRule("footway == CROSSING", 1.1);
                builder.AddPriorityRule("crossing == TRAFFIC_SIGNALS || crossing == MARKED", 1.2);
                builder.AddPriorityRule("surface == PAVED || surface == ASPHALT || surface == CONCRETE", 1.3);
                builder.AddPriorityRule("smoothness == EXCELLENT || smoothness == GOOD", 1.2);

                // Speed reductions on rough surfaces
                builder.AddSpeedMultiply("surface == COBBLESTONE || surface == PAVING_STONES", 0.7);
                builder.AddSpeedMultiply("smoothness == BAD || smoothness == VERY_BAD || smoothness == HORRIBLE", 0.6);
            }
            else if (string.Equals(profile.Preset, "crutches", System.StringComparison.OrdinalIgnoreCase) || profile.StairsPenalty.HasValue)
            {
                // Surface and smoothness requirements for crutches and walking difficulties
                builder.AddPriorityRule("smoothness == IMPASSABLE", 0.0);
                builder.AddPriorityRule("surface == SAND || surface == OTHER", 0.1);
                builder.AddPriorityRule("surface == COBBLESTONE", 0.4);
                builder.AddPriorityRule("surface == GRAVEL || surface == UNPAVED || surface == DIRT", 0.4);
                builder.AddPriorityRule("smoothness == BAD", 0.5);
                builder.AddPriorityRule(
                    "smoothness == VERY_BAD || smoothness == HORRIBLE || smoothness == VERY_HORRIBLE",
                    0.1);

                builder.AddPriorityRule("footway == SIDEWALK", 1.3);
                builder.AddPriorityRule("footway == CROSSING", 1.1);
                builder.AddPriorityRule("crossing == TRAFFIC_SIGNALS || crossing == MARKED", 1.2);
                builder.AddPriorityRule("surface == PAVED || surface == ASPHALT || surface == CONCRETE", 1.5);
                builder.AddPriorityRule("smoothness == EXCELLENT || smoothness == GOOD", 1.3);

                builder.AddSpeedMultiply("surface == COBBLESTONE || surface == PAVING_STONES", 0.6);
                builder.AddSpeedMultiply("surface == GRAVEL || surface == UNPAVED", 0.5);
                builder.AddSpeedMultiply("smoothness == BAD || smoothness == VERY_BAD || smoothness == HORRIBLE", 0.5);
            }
        }
    }
}

