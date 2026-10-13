// <copyright file="UserProfile.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;

namespace AstreaEngine
{
    /// <summary>
    /// Represents the user's accessibility profile, physical constraints, and navigation preferences.
    /// </summary>
    public class UserProfile
    {
        private delegate bool TryGetFunc<T>(JsonObject obj, string propertyName, out T value);

        private interface IPropertyMapping
        {
            void Apply(JsonObject obj, UserProfile profile);
        }

        private sealed class PropertyMapping<T> : IPropertyMapping
        {
            public string PropertyName { get; }

            public Action<UserProfile, T> Setter { get; }

            public TryGetFunc<T> Getter { get; }

            public PropertyMapping(string propertyName, Action<UserProfile, T> setter, TryGetFunc<T> getter)
            {
                PropertyName = propertyName;
                Setter = setter;
                Getter = getter;
            }

            public void Apply(JsonObject obj, UserProfile profile)
            {
                if (Getter(obj, PropertyName, out var value))
                {
                    Setter(profile, value);
                }
            }
        }

        private static readonly IPropertyMapping[] PropertyMappings =
        {
            Map("profile", (profile, v) => ApplyPreset(profile, v),TryGetString),
            Map("preset", (profile, v) => ApplyPreset(profile, v), TryGetString),
            Map("name", (profile, v) => ApplyPreset(profile, v), TryGetString),

            Map("allow_stairs", (profile, v) => profile.CanTakeStairs = v, TryGetBoolean),
            Map("can_take_stairs", (profile, v) => profile.CanTakeStairs = v, TryGetBoolean),
            Map("stairs", (profile, v) => profile.CanTakeStairs = v, TryGetBoolean),
            Map("avoid_stairs", (profile, v) => profile.CanTakeStairs = !v, TryGetBoolean),
            Map("no_stairs", (profile, v) => profile.CanTakeStairs = !v, TryGetBoolean),
            Map("stairs_penalty", (profile, v) => profile.StairsPenalty = v, TryGetDouble),

            Map("max_slope", (profile, v) => { profile.MaxSlope = v; profile.AvoidSlopes = true; }, TryGetDouble),
            Map("slope", (profile, v) => { profile.MaxSlope = v; profile.AvoidSlopes = true; }, TryGetDouble),
            Map("avoid_slopes", (profile, v) => profile.AvoidSlopes = v, TryGetBoolean),
            Map("avoid_slope", (profile, v) => profile.AvoidSlopes = v, TryGetBoolean),
            Map("slope", (profile, v) => profile.AvoidSlopes = !v, TryGetBoolean),
            Map("slopes", (profile, v) => profile.AvoidSlopes = !v, TryGetBoolean),

            Map("has_wheels", (profile, v) => profile.HasWheels = v, TryGetBoolean),
            Map("wheels", (profile, v) => profile.HasWheels = v, TryGetBoolean),
            Map("wheel", (profile, v) => profile.HasWheels = v, TryGetBoolean),

            Map("blind", (profile, v) => profile.Blindness = v, TryGetBoolean),
            Map("blindness", (profile, v) => profile.Blindness = v, TryGetBoolean),

            Map("width", (profile, v) => profile.Width = v, TryGetDouble),
            Map("max_width", (profile, v) => profile.Width = v, TryGetDouble),
            Map("length", (profile, v) => profile.Length = v, TryGetDouble),
            Map("max_length", (profile, v) => profile.Length = v, TryGetDouble),

            Map("speed", (profile, v) => profile.MaxSpeed = v, TryGetDouble),
            Map("max_speed", (profile, v) => profile.MaxSpeed = v, TryGetDouble),

            Map("distance_influence", (profile, v) => profile.DistanceInfluence = v, TryGetInt),
        };

        public string? Preset { get; set; }

        public bool CanTakeStairs { get; set; } = true;

        public double? StairsPenalty { get; set; }

        public bool AvoidSlopes { get; set; }

        public double? MaxSlope { get; set; }

        public bool HasWheels { get; set; }

        public bool Blindness { get; set; }

        public double? Width { get; set; }

        public double? Length { get; set; }

        public double? MaxSpeed { get; set; }

        public int? DistanceInfluence { get; set; }

        /// <summary>
        /// Gets additional arbitrary properties passed by the backend.
        /// </summary>
        public Dictionary<string, object> ExtraProperties { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Parses a <see cref="JsonNode"/> representing the user profile JSON into a <see cref="UserProfile"/>.
        /// Automatically applies defaults based on preset names and allows fine-grained overrides.
        /// </summary>
        /// <param name="json">The input JSON node.</param>
        /// <returns>A populated <see cref="UserProfile"/> instance.</returns>
        public static UserProfile Parse(JsonNode? json)
        {
            var profile = new UserProfile();

            if (json == null)
            {
                return profile;
            }

            if (json is JsonValue val && val.TryGetValue<string>(out var singleProfileName))
            {
                ApplyPreset(profile, singleProfileName);
                return profile;
            }

            if (json is not JsonObject obj)
            {
                return profile;
            }

            foreach (var mapping in PropertyMappings)
            {
                mapping.Apply(obj, profile);
            }

            // Check nested dimensions object if present e.g. "dimensions": { "width": 0.8, "length": 1.2 }
            if (obj["dimensions"] is JsonObject dimsObj)
            {
                foreach (var mapping in PropertyMappings)
                {
                    mapping.Apply(dimsObj, profile);
                }
            }

            // Populate ExtraProperties with arbitrary unmapped fields
            foreach (var kvp in obj)
            {
                if (!KnownPropertyNames.Contains(kvp.Key) && kvp.Value != null)
                {
                    profile.ExtraProperties[kvp.Key] = kvp.Value.ToString();
                }
            }

            return profile;
        }

        private static readonly HashSet<string> KnownPropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "profile", "preset", "name",
            "allow_stairs", "can_take_stairs", "stairs", "avoid_stairs", "no_stairs", "stairs_penalty",
            "avoid_slopes", "avoid_slope", "slope", "slopes", "max_slope",
            "has_wheels", "wheels", "wheel",
            "blind", "blindness",
            "width", "max_width", "length", "max_length",
            "speed", "max_speed",
            "distance_influence", "dimensions"
        };

        private static PropertyMapping<bool> Map(string propertyName, Action<UserProfile, bool> setter, TryGetFunc<bool> getter)
            => new(propertyName, setter, getter);

        private static PropertyMapping<double> Map(string propertyName, Action<UserProfile, double> setter, TryGetFunc<double> getter)
            => new(propertyName, setter, getter);

        private static PropertyMapping<int> Map(string propertyName, Action<UserProfile, int> setter, TryGetFunc<int> getter)
            => new(propertyName, setter, getter);

        private static PropertyMapping<string> Map(string propertyName, Action<UserProfile, string> setter, TryGetFunc<string> getter)
            => new(propertyName, setter, getter);

        private static void ApplyPreset(UserProfile profile, string preset)
        {
            var normalized = preset.Trim().ToLowerInvariant();
            profile.Preset = normalized;

            switch (normalized)
            {
                case "wheelchair":
                    profile.HasWheels = true;
                    profile.CanTakeStairs = false;
                    profile.AvoidSlopes = true;
                    profile.MaxSlope = 8.0;
                    profile.Width = 0.9;
                    profile.MaxSpeed = 3.5;
                    profile.DistanceInfluence = 70;
                    break;

                case "blind":
                    profile.Blindness = true;
                    profile.CanTakeStairs = true;
                    profile.HasWheels = false;
                    profile.AvoidSlopes = false;
                    profile.MaxSpeed = 3.5;
                    profile.DistanceInfluence = 80;
                    break;

                case "crutches":
                    profile.HasWheels = false;
                    profile.CanTakeStairs = true;
                    profile.StairsPenalty = 0.4;
                    profile.AvoidSlopes = true;
                    profile.MaxSlope = 10.0;
                    profile.MaxSpeed = 3.0;
                    profile.DistanceInfluence = 70;
                    break;

                case "walking":
                case "foot":
                    profile.HasWheels = false;
                    profile.CanTakeStairs = true;
                    profile.AvoidSlopes = false;
                    profile.MaxSpeed = 5.0;
                    profile.DistanceInfluence = 60;
                    break;
            }
        }

        private static JsonNode? GetProperty(JsonObject obj, string propertyName)
        {
            if (obj.TryGetPropertyValue(propertyName, out var direct))
            {
                return direct;
            }

            foreach (var kvp in obj)
            {
                if (string.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private static bool TryGetBoolean(JsonObject obj, string propertyName, out bool result)
        {
            var node = GetProperty(obj, propertyName);
            if (node is JsonValue val)
            {
                if (val.TryGetValue<bool>(out result))
                {
                    return true;
                }

                var str = val.ToString();
                if (bool.TryParse(str, out result))
                {
                    return true;
                }

                if (string.Equals(str, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    return true;
                }

                if (string.Equals(str, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(str, "no", StringComparison.OrdinalIgnoreCase))
                {
                    result = false;
                    return true;
                }
            }

            result = false;
            return false;
        }

        private static bool TryGetDouble(JsonObject obj, string propertyName, out double result)
        {
            var node = GetProperty(obj, propertyName);
            if (node is JsonValue val)
            {
                if (val.TryGetValue<double>(out result))
                {
                    return true;
                }

                if (double.TryParse(val.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                {
                    return true;
                }
            }

            result = 0.0;
            return false;
        }

        private static bool TryGetInt(JsonObject obj, string propertyName, out int result)
        {
            var node = GetProperty(obj, propertyName);
            if (node is JsonValue val)
            {
                if (val.TryGetValue<int>(out result))
                {
                    return true;
                }

                if (int.TryParse(val.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    return true;
                }
            }

            result = 0;
            return false;
        }

        private static bool TryGetString(JsonObject obj, string propertyName, out string result)
        {
            var node = GetProperty(obj, propertyName);
            if (node != null)
            {
                result = node.ToString();
                return !string.IsNullOrWhiteSpace(result);
            }

            result = string.Empty;
            return false;
        }
    }
}
