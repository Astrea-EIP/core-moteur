// <copyright file="BlindnessFeatureRule.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

namespace AstreaEngine.Features
{
    /// <summary>
    /// Configures routing for visually impaired and blind users, prioritizing tactile sidewalks,
    /// audible signals at crossings, and avoiding high-speed roads, tunnels, and unmarked crossings.
    /// </summary>
    public class BlindnessFeatureRule : IProfileFeatureRule
    {
        /// <inheritdoc />
        public int Order => 40;

        /// <inheritdoc />
        public void Apply(UserProfile profile, CustomModelBuilder builder)
        {
            if (!profile.Blindness)
            {
                return;
            }

            // Protected pedestrian infrastructure
            builder.AddPriorityRule("footway == SIDEWALK", 1.4);
            builder.AddPriorityRule("footway == CROSSING", 1.2);
            builder.AddPriorityRule("crossing == TRAFFIC_SIGNALS", 1.5);

            // Penalize unmarked or uncontrolled crossings (safety risk without audio/tactile cues)
            builder.AddPriorityRule("crossing == UNMARKED || crossing == UNCONTROLLED", 0.4);

            // Avoid vehicular roads and disorientation in tunnels/bridges
            builder.AddPriorityRule("road_class == PRIMARY || road_class == SECONDARY || road_class == TERTIARY", 0.5);
            builder.AddPriorityRule("road_class == TRUNK || road_class == MOTORWAY", 0.1);
            builder.AddPriorityRule("road_environment == TUNNEL || road_environment == BRIDGE", 0.5);

            // Surface predictability for cane and guide dogs
            builder.AddPriorityRule("surface == PAVED || surface == ASPHALT || surface == CONCRETE", 1.3);
            builder.AddPriorityRule("surface == GRAVEL || surface == UNPAVED || surface == SAND", 0.4);

            if (profile.CanTakeStairs)
            {
                builder.AddPriorityRule("road_class == STEPS", 0.7);
                builder.AddSpeedMultiply("road_class == STEPS", 0.5);
            }

            // Speed cautions
            builder.AddSpeedMultiply("crossing == UNMARKED", 0.6);
            builder.AddSpeedMultiply("surface == GRAVEL || surface == UNPAVED", 0.7);
        }
    }
}
