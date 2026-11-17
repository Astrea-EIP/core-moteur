// <copyright file="DimensionsFeatureRule.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System;
using System.Globalization;

namespace AstreaEngine.Features
{
    /// <summary>
    /// Enforces clearance and width constraints for large objects, wheelchairs, cargo strollers, etc.
    /// based on OSM max_width tags.
    /// </summary>
    public class DimensionsFeatureRule : IProfileFeatureRule
    {
        /// <inheritdoc />
        public int Order => 50;

        /// <inheritdoc />
        public void Apply(UserProfile profile, CustomModelBuilder builder)
        {
            if (!profile.Width.HasValue && !profile.Length.HasValue)
            {
                return;
            }

            // Determine minimum passage width required
            double effectiveWidth = profile.Width ?? (profile.Length!.Value * 0.75);

            if (effectiveWidth <= 0.0)
            {
                return;
            }

            // Impassable if tagged max_width is narrower than user/object width
            builder.AddPriorityRule(
                $"max_width < {effectiveWidth.ToString("0.##", CultureInfo.InvariantCulture)}",
                0.0);

            // Favor ways that explicitly offer comfortable clearance
            var comfortableWidth = effectiveWidth + 0.5;
            builder.AddPriorityRule(
                $"max_width >= {comfortableWidth.ToString("0.##", CultureInfo.InvariantCulture)}",
                1.2);
        }
    }
}
