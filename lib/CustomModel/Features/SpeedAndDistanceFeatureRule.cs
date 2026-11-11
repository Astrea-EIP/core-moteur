// <copyright file="SpeedAndDistanceFeatureRule.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

namespace AstreaEngine.Features
{
    /// <summary>
    /// Configures global walking/rolling speed limits and distance influence for route optimization.
    /// </summary>
    public class SpeedAndDistanceFeatureRule : IProfileFeatureRule
    {
        /// <inheritdoc />
        public int Order => 100;

        /// <inheritdoc />
        public void Apply(UserProfile profile, CustomModelBuilder builder)
        {
            // Speed limits
            if (profile.MaxSpeed.HasValue)
            {
                builder.AddSpeedLimit("true", profile.MaxSpeed.Value);
            }
            else if (profile.Blindness)
            {
                builder.AddSpeedLimit("true", 3.5);
            }

            // Distance influence: trade-off between extra detour and comfortable/safe paths
            if (profile.DistanceInfluence.HasValue)
            {
                builder.SetDistanceInfluence(profile.DistanceInfluence.Value);
            }
            else if (profile.Blindness)
            {
                builder.SetDistanceInfluence(80);
            }
            else if (profile.HasWheels || profile.AvoidSlopes)
            {
                builder.SetDistanceInfluence(70);
            }
            else
            {
                builder.SetDistanceInfluence(60);
            }
        }
    }
}
