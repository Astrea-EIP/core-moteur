// <copyright file="IProfileFeatureRule.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

namespace AstreaEngine.Features
{
    /// <summary>
    /// Contract for modular accessibility feature rules that translate user profile criteria
    /// into GraphHopper Custom Model rules.
    /// </summary>
    public interface IProfileFeatureRule
    {
        /// <summary>
        /// Gets the execution order of the rule. Lower numbers execute earlier.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Applies custom model rules (priority, speed, distance influence) based on the user's profile.
        /// </summary>
        /// <param name="profile">The user profile to inspect.</param>
        /// <param name="builder">The builder to append rules to.</param>
        void Apply(UserProfile profile, CustomModelBuilder builder);
    }
}
