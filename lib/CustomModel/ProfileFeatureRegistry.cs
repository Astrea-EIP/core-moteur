// <copyright file="ProfileFeatureRegistry.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using AstreaEngine.Features;

namespace AstreaEngine
{
    /// <summary>
    /// Registry and factory that manages accessibility feature rules and constructs
    /// GraphHopper custom models. Allows adding new criteria dynamically.
    /// </summary>
    public class ProfileFeatureRegistry
    {
        private static readonly Lazy<ProfileFeatureRegistry> DefaultInstance = new(() =>
        {
            var registry = new ProfileFeatureRegistry();
            registry.RegisterRule(new StairsFeatureRule());
            registry.RegisterRule(new SlopeFeatureRule());
            registry.RegisterRule(new WheelFeatureRule());
            registry.RegisterRule(new BlindnessFeatureRule());
            registry.RegisterRule(new DimensionsFeatureRule());
            registry.RegisterRule(new SpeedAndDistanceFeatureRule());
            return registry;
        });

        private readonly object _syncLock = new();
        private readonly List<IProfileFeatureRule> _rules = new();

        /// <summary>
        /// Gets the singleton default registry containing all built-in accessibility rules.
        /// </summary>
        /// <remarks>
        /// <b>Do not call <see cref="RegisterRule"/> on this instance at runtime.</b>
        /// <see cref="Default"/> is a shared, lazy-initialized singleton; mutating it will affect
        /// all concurrent requests. To use custom rules, create a separate <see cref="ProfileFeatureRegistry"/>
        /// instance via <c>new ProfileFeatureRegistry()</c> and register rules on that instance instead.
        /// </remarks>
        public static ProfileFeatureRegistry Default => DefaultInstance.Value;

        /// <summary>
        /// Registers a new or custom feature rule with the registry.
        /// </summary>
        /// <param name="rule">The feature rule to register.</param>
        public void RegisterRule(IProfileFeatureRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            lock (_syncLock)
            {
                _rules.Add(rule);
                _rules.Sort((a, b) => a.Order.CompareTo(b.Order));
            }
        }

        /// <summary>
        /// Unregisters all rules of the specified type from the registry.
        /// </summary>
        /// <typeparam name="T">The type of the feature rule to remove.</typeparam>
        /// <returns>True if at least one rule was removed; otherwise, false.</returns>
        public bool UnregisterRule<T>()
            where T : IProfileFeatureRule
        {
            lock (_syncLock)
            {
                return _rules.RemoveAll(r => r is T) > 0;
            }
        }

        /// <summary>
        /// Gets the currently registered rules.
        /// </summary>
        public IReadOnlyList<IProfileFeatureRule> GetRules()
        {
            lock (_syncLock)
            {
                return _rules.ToArray();
            }
        }

        /// <summary>
        /// Constructs a GraphHopper custom model from a user accessibility profile.
        /// </summary>
        /// <param name="profile">The user profile.</param>
        /// <returns>A <see cref="JsonObject"/> representing the custom model.</returns>
        public JsonObject BuildCustomModel(UserProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            IProfileFeatureRule[] activeRules;
            lock (_syncLock)
            {
                activeRules = _rules.ToArray();
            }

            var builder = new CustomModelBuilder();

            foreach (var rule in activeRules)
            {
                rule.Apply(profile, builder);
            }

            return builder.Build();
        }
    }
}
