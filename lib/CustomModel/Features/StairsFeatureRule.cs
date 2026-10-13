// <copyright file="StairsFeatureRule.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

namespace AstreaEngine.Features
{
    /// <summary>
    /// Evaluates user ability to take stairs and generates corresponding priority and speed rules.
    /// </summary>
    public class StairsFeatureRule : IProfileFeatureRule
    {
        /// <inheritdoc />
        public int Order => 10;

        /// <inheritdoc />
        public void Apply(UserProfile profile, CustomModelBuilder builder)
        {
            if (!profile.CanTakeStairs)
            {
                // Impassable for users who cannot take stairs (wheelchair, strollers, etc.)
                builder.AddPriorityRule("road_class == STEPS", 0.0);
            }
            else if (profile.StairsPenalty.HasValue)
            {
                // Penalized for users who take stairs with difficulty (crutches, limited mobility)
                builder.AddPriorityRule("road_class == STEPS", profile.StairsPenalty.Value);
                builder.AddSpeedMultiply("road_class == STEPS", 0.3);
            }
            else if (!profile.Blindness)
            {
                // Normal walking speed reduction on stairs (blindness rule configures dedicated speed/priority)
                builder.AddSpeedMultiply("road_class == STEPS", 0.6);
            }
        }
    }
}
