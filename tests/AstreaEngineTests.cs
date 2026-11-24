using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AstreaEngine;
using AstreaEngine.Features;
using Xunit;

namespace AstreaEngineTests
{
    public class AstreaEngineUnitTests
    {
        public String GraphhopperUrl = "http://localhost:8989";

        [Fact]
        [Trait("Category", "Integration")]
        public void TestDockerUp()
        {
            var healthCheck = AstreaEngine.AstreaEngine.AstreaHealth(GraphhopperUrl);
            Assert.NotNull(healthCheck);
            Assert.Contains("\"status\":\"OK\"", healthCheck, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TestAstreaRouteWithInvalidPoints()
        {
            var points = new List<PointResponse>
            {
                new PointResponse { Lat = 47.21, Lng = -1.55 }
            };

            var exception = Assert.Throws<ValidationException>(() =>
            {
                AstreaEngine.AstreaEngine.AstreaRoute(
                    GraphhopperUrl,
                    points,
                    "{\"profile\":\"foot\"}"
                );
            });

            Assert.Contains("at least 2 points", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TestAstreaRouteAsyncWithInvalidPoints()
        {
            var points = new List<PointResponse>();

            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await AstreaEngine.AstreaEngine.AstreaRouteAsync(
                    GraphhopperUrl,
                    points,
                    ""
                );
            });

            Assert.Contains("at least 2 points", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task TestAstreaRouteAsyncWithCustomModel()
        {
            var points = new List<PointResponse>
            {
                new PointResponse { Lat = 47.21, Lng = -1.55 },
                new PointResponse { Lat = 47.22, Lng = -1.54 }
            };

            var result = await AstreaEngine.AstreaEngine.AstreaRouteAsync(
                GraphhopperUrl,
                points,
                "{\"profile\":\"wheelchair\"}"
            );

            Assert.NotNull(result);
            Assert.IsType<RouteResponse>(result);
            Assert.NotEmpty(result.Points);
        }

        [Fact]
        public async Task TestAstreaRouteAsyncWithInvalidUserJson()
        {
            var points = new List<PointResponse>
            {
                new PointResponse { Lat = 47.21, Lng = -1.55 },
                new PointResponse { Lat = 47.22, Lng = -1.54 }
            };

            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await AstreaEngine.AstreaEngine.AstreaRouteAsync(
                    GraphhopperUrl,
                    points,
                    "invalid json"
                );
            });

            Assert.Contains("Invalid user JSON", exception.Message);
        }

        [Fact]
        public async Task TestRoutesAsyncWithMalformedCoordinateThrowsValidation()
        {
            var points = new List<string> { "47.21,-1.55", "invalid-coord" };
            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await Routes.RouteAsync(GraphhopperUrl, points, "{}");
            });

            Assert.Contains("Invalid coordinate format", exception.Message);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task TestAstreaRouteAsyncWithNullOrEmptyUserJsonDoesNotThrowValidationException()
        {
            var points = new List<PointResponse>
            {
                new PointResponse { Lat = 47.21, Lng = -1.55 },
                new PointResponse { Lat = 47.22, Lng = -1.54 }
            };

            var result = await AstreaEngine.AstreaEngine.AstreaRouteAsync(GraphhopperUrl, points, "");
            Assert.NotNull(result);
            Assert.IsType<RouteResponse>(result);
            Assert.NotEmpty(result.Points);
        }
    }

    public class CustomUserModelUnitTests
    {
        [Fact]
        public void UserProfile_Parse_DefaultsWhenEmptyOrNull()
        {
            var profileFromNull = UserProfile.Parse(null);
            Assert.True(profileFromNull.CanTakeStairs);
            Assert.False(profileFromNull.AvoidSlopes);
            Assert.False(profileFromNull.HasWheels);
            Assert.False(profileFromNull.Blindness);

            var profileFromEmpty = UserProfile.Parse(new JsonObject());
            Assert.True(profileFromEmpty.CanTakeStairs);
            Assert.False(profileFromEmpty.AvoidSlopes);
            Assert.False(profileFromEmpty.HasWheels);
            Assert.False(profileFromEmpty.Blindness);
        }

        [Theory]
        [InlineData("wheelchair", true, false, true, 8.0, 0.9, 70)]
        [InlineData("blind", false, true, false, null, null, 80)]
        [InlineData("crutches", false, true, true, 10.0, null, 70)]
        [InlineData("walking", false, true, false, null, null, 60)]
        [InlineData("foot", false, true, false, null, null, 60)]
        public void UserProfile_Parse_StandardPresets(
            string presetName,
            bool expectedWheels,
            bool expectedStairs,
            bool expectedAvoidSlopes,
            double? expectedMaxSlope,
            double? expectedWidth,
            int expectedDistanceInfluence)
        {
            var json = JsonNode.Parse($"{{\"profile\": \"{presetName}\"}}");
            var profile = UserProfile.Parse(json);

            Assert.Equal(presetName, profile.Preset);
            Assert.Equal(expectedWheels, profile.HasWheels);
            Assert.Equal(expectedStairs, profile.CanTakeStairs);
            Assert.Equal(expectedAvoidSlopes, profile.AvoidSlopes);
            Assert.Equal(expectedMaxSlope, profile.MaxSlope);
            Assert.Equal(expectedWidth, profile.Width);
            Assert.Equal(expectedDistanceInfluence, profile.DistanceInfluence);

            if (presetName == "blind")
            {
                Assert.True(profile.Blindness);
            }

            if (presetName == "crutches")
            {
                Assert.Equal(0.4, profile.StairsPenalty);
            }
        }

        [Fact]
        public void UserProfile_Parse_PresetWithGranularOverrides()
        {
            var json = JsonNode.Parse("{\"profile\": \"wheelchair\", \"avoid_stairs\": false, \"width\": 0.75}");
            var profile = UserProfile.Parse(json);

            Assert.True(profile.HasWheels);
            Assert.True(profile.CanTakeStairs);
            Assert.Equal(0.75, profile.Width);
            Assert.True(profile.AvoidSlopes);
        }

        [Fact]
        public void UserProfile_Parse_GranularCriteriaWithoutPreset()
        {
            var json = JsonNode.Parse(@"
            {
                ""avoid_stairs"": true,
                ""avoid_slopes"": true,
                ""max_slope"": 6.0,
                ""has_wheels"": true,
                ""blindness"": true,
                ""dimensions"": {
                    ""width"": 0.85,
                    ""length"": 1.2
                },
                ""max_speed"": 2.8,
                ""distance_influence"": 90
            }");

            var profile = UserProfile.Parse(json);

            Assert.False(profile.CanTakeStairs);
            Assert.True(profile.AvoidSlopes);
            Assert.Equal(6.0, profile.MaxSlope);
            Assert.True(profile.HasWheels);
            Assert.True(profile.Blindness);
            Assert.Equal(0.85, profile.Width);
            Assert.Equal(1.2, profile.Length);
            Assert.Equal(2.8, profile.MaxSpeed);
            Assert.Equal(90, profile.DistanceInfluence);
        }

        [Fact]
        public void UserProfile_Parse_FlexibleBooleanFormats()
        {
            var json = JsonNode.Parse("{\"has_wheels\": \"yes\", \"avoid_stairs\": \"yes\", \"avoid_slopes\": \"1\"}");
            var profile = UserProfile.Parse(json);

            Assert.True(profile.HasWheels);
            Assert.False(profile.CanTakeStairs);
            Assert.True(profile.AvoidSlopes);
        }

        [Fact]
        public void CustomModel_StairsRule_DisablesStepsWhenCannotTakeStairs()
        {
            var profile = new UserProfile { CanTakeStairs = false };
            var builder = new CustomModelBuilder();
            new StairsFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            Assert.NotNull(priority);
            var stepsRule = priority.FirstOrDefault(r => r?["if"]?.ToString() == "road_class == STEPS");
            Assert.NotNull(stepsRule);
            Assert.Equal("0", stepsRule["multiply_by"]?.ToString());
        }

        [Fact]
        public void CustomModel_StairsRule_AppliesPenaltyForCrutches()
        {
            var profile = new UserProfile { CanTakeStairs = true, StairsPenalty = 0.4 };
            var builder = new CustomModelBuilder();
            new StairsFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();
            var speed = model["speed"]?.AsArray();

            Assert.NotNull(priority);
            var stepsRule = priority.FirstOrDefault(r => r?["if"]?.ToString() == "road_class == STEPS");
            Assert.NotNull(stepsRule);
            Assert.Equal("0.4", stepsRule["multiply_by"]?.ToString());

            Assert.NotNull(speed);
            var speedRule = speed.FirstOrDefault(r => r?["if"]?.ToString() == "road_class == STEPS");
            Assert.NotNull(speedRule);
            Assert.Equal("0.3", speedRule["multiply_by"]?.ToString());
        }

        [Fact]
        public void CustomModel_SlopeRule_RestrictsAverageSlope()
        {
            var profile = new UserProfile { AvoidSlopes = true, MaxSlope = 8.0 };
            var builder = new CustomModelBuilder();
            new SlopeFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            Assert.NotNull(priority);
            var impassableSlopeRule = priority.FirstOrDefault(r => r?["if"]?.ToString() == "average_slope > 8");
            Assert.NotNull(impassableSlopeRule);
            Assert.Equal("0", impassableSlopeRule["multiply_by"]?.ToString());

            var moderateSlopeRule = priority.FirstOrDefault(r => r?["if"]?.ToString() == "average_slope > 4.8");
            Assert.NotNull(moderateSlopeRule);
            Assert.Equal("0.3", moderateSlopeRule["multiply_by"]?.ToString());
        }

        [Fact]
        public void CustomModel_WheelRule_PenalizesCobblestoneAndImpassable()
        {
            var profile = new UserProfile { HasWheels = true };
            var builder = new CustomModelBuilder();
            new WheelFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            Assert.NotNull(priority);
            var impassable = priority.FirstOrDefault(r => r?["if"]?.ToString() == "smoothness == IMPASSABLE");
            Assert.NotNull(impassable);
            Assert.Equal("0", impassable["multiply_by"]?.ToString());

            var cobblestone = priority.FirstOrDefault(r => r?["if"]?.ToString() == "surface == COBBLESTONE");
            Assert.NotNull(cobblestone);
            Assert.Equal("0.3", cobblestone["multiply_by"]?.ToString());

            var sidewalk = priority.FirstOrDefault(r => r?["if"]?.ToString() == "footway == SIDEWALK");
            Assert.NotNull(sidewalk);
            Assert.Equal("1.3", sidewalk["multiply_by"]?.ToString());
        }

        [Fact]
        public void CustomModel_BlindnessRule_PrefersSignalsAndAvoidsUnmarkedCrossings()
        {
            var profile = new UserProfile { Blindness = true, CanTakeStairs = true };
            var builder = new CustomModelBuilder();
            new BlindnessFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            Assert.NotNull(priority);
            var signals = priority.FirstOrDefault(r => r?["if"]?.ToString() == "crossing == TRAFFIC_SIGNALS");
            Assert.NotNull(signals);
            Assert.Equal("1.5", signals["multiply_by"]?.ToString());

            var unmarked = priority.FirstOrDefault(r => r?["if"]?.ToString() == "crossing == UNMARKED || crossing == UNCONTROLLED");
            Assert.NotNull(unmarked);
            Assert.Equal("0.4", unmarked["multiply_by"]?.ToString());
        }

        [Fact]
        public void CustomModel_DimensionsRule_SetsMaxWidthBoundary()
        {
            var profile = new UserProfile { Width = 0.8 };
            var builder = new CustomModelBuilder();
            new DimensionsFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            Assert.NotNull(priority);
            var widthBoundary = priority.FirstOrDefault(r => r?["if"]?.ToString() == "max_width < 0.8");
            Assert.NotNull(widthBoundary);
            Assert.Equal("0", widthBoundary["multiply_by"]?.ToString());

            var clearance = priority.FirstOrDefault(r => r?["if"]?.ToString() == "max_width >= 1.3");
            Assert.NotNull(clearance);
            Assert.Equal("1.2", clearance["multiply_by"]?.ToString());
        }

        [Fact]
        public void CustomUserModel_BuildCustomModel_EndToEndRequestPayload()
        {
            var jsonBody = new JsonObject();
            var userJson = JsonNode.Parse("{\"profile\": \"wheelchair\"}");

            CustomUserModel.BuildCustomModel(jsonBody, userJson!);

            Assert.Equal("foot", jsonBody["profile"]?.ToString());
            Assert.Equal(true, jsonBody["ch.disable"]?.GetValue<bool>());

            var customModel = jsonBody["custom_model"]?.AsObject();
            Assert.NotNull(customModel);
            Assert.Equal(70, customModel["distance_influence"]?.GetValue<int>());

            var priority = customModel["priority"]?.AsArray();
            Assert.NotNull(priority);
            Assert.True(priority.Count > 0);

            var stairsRule = priority.FirstOrDefault(r => r?["if"]?.ToString() == "road_class == STEPS");
            Assert.NotNull(stairsRule);
            Assert.Equal("0", stairsRule["multiply_by"]?.ToString());
        }

        [Fact]
        public void CustomModel_WheelRule_AppliesSurfaceRulesForCrutches()
        {
            var profile = new UserProfile { Preset = "crutches", HasWheels = false, StairsPenalty = 0.4 };
            var builder = new CustomModelBuilder();
            new WheelFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            Assert.NotNull(priority);
            var cobblestone = priority.FirstOrDefault(r => r?["if"]?.ToString() == "surface == COBBLESTONE");
            Assert.NotNull(cobblestone);
            Assert.Equal("0.4", cobblestone["multiply_by"]?.ToString());

            var impassable = priority.FirstOrDefault(r => r?["if"]?.ToString() == "smoothness == IMPASSABLE");
            Assert.NotNull(impassable);
            Assert.Equal("0", impassable["multiply_by"]?.ToString());
        }

        [Fact]
        public void UserProfile_Parse_ThrowsOnUnknownProperty()
        {
            var json = JsonNode.Parse("{\"profile\": \"wheelchair\", \"custom_tag\": \"preferred\"}");
            var ex = Assert.Throws<ProfileValidationException>(() => UserProfile.Parse(json));
            Assert.Contains("Unknown property 'custom_tag'", ex.Message);
        }

        [Fact]
        public void ProfileFeatureRegistry_AllowsRegisteringCustomRules()
        {
            var registry = new ProfileFeatureRegistry();
            registry.RegisterRule(new TestCustomRule());

            var profile = new UserProfile();
            var model = registry.BuildCustomModel(profile);

            var priority = model["priority"]?.AsArray();
            Assert.NotNull(priority);
            var testRule = priority.FirstOrDefault(r => r?["if"]?.ToString() == "road_environment == TUNNEL");
            Assert.NotNull(testRule);
            Assert.Equal("0", testRule["multiply_by"]?.ToString());
        }

        [Fact]
        public void ProfileFeatureRegistry_UnregisterRule_RemovesRuleSuccessfully()
        {
            var registry = new ProfileFeatureRegistry();
            registry.RegisterRule(new TestCustomRule());
            Assert.Single(registry.GetRules());

            var removed = registry.UnregisterRule<TestCustomRule>();
            Assert.True(removed);
            Assert.Empty(registry.GetRules());
        }

        private sealed class TestCustomRule : IProfileFeatureRule
        {
            public int Order => 1;

            public void Apply(UserProfile profile, CustomModelBuilder builder)
            {
                builder.AddPriorityRule("road_environment == TUNNEL", 0.0);
            }
        }
    }

    public class CustomModelBuilderUnitTests
    {
        [Fact]
        public void CustomModelBuilder_FormatsNumbersCultureInvariantly()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                // French culture uses comma as decimal separator (e.g. 1,5 instead of 1.5)
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

                var builder = new CustomModelBuilder();
                builder.AddPriorityRule("max_width < 1.5", 0.25);
                builder.AddSpeedLimit("true", 3.5);
                builder.AddSpeedMultiply("average_slope > 4.8", 0.65);
                builder.SetDistanceInfluence(85);

                var model = builder.Build();

                var priority = model["priority"]?.AsArray();
                Assert.NotNull(priority);
                Assert.Equal("0.25", priority[0]?["multiply_by"]?.ToString());

                var speed = model["speed"]?.AsArray();
                Assert.NotNull(speed);
                Assert.Equal("3.5", speed[0]?["limit_to"]?.ToString());
                Assert.Equal("0.65", speed[1]?["multiply_by"]?.ToString());

                Assert.Equal(85, model["distance_influence"]?.GetValue<int>());
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }

    public class FeatureRulesExhaustiveUnitTests
    {
        [Fact]
        public void StairsRule_BlindProfile_SkipsDefaultSpeedMultiplierToAvoidDuplication()
        {
            var profile = new UserProfile { CanTakeStairs = true, Blindness = true };
            var builder = new CustomModelBuilder();
            new StairsFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var speed = model["speed"]?.AsArray();

            // When Blindness is true, StairsFeatureRule should not add the 0.6 speed multiplier
            Assert.NotNull(speed);
            Assert.Empty(speed);
        }

        [Fact]
        public void StairsRule_NormalWalking_AppliesDefaultSpeedMultiplier()
        {
            var profile = new UserProfile { CanTakeStairs = true, Blindness = false, StairsPenalty = null };
            var builder = new CustomModelBuilder();
            new StairsFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var speed = model["speed"]?.AsArray();

            Assert.NotNull(speed);
            var stepsSpeed = speed.FirstOrDefault(s => s?["if"]?.ToString() == "road_class == STEPS");
            Assert.NotNull(stepsSpeed);
            Assert.Equal("0.6", stepsSpeed["multiply_by"]?.ToString());
        }

        [Fact]
        public void SlopeRule_AvoidSlopesFalse_AppliesWalkingSlopeSpeedAdjustments()
        {
            var profile = new UserProfile { AvoidSlopes = false };
            var builder = new CustomModelBuilder();
            new SlopeFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var speed = model["speed"]?.AsArray();

            Assert.NotNull(speed);
            var slope8 = speed.FirstOrDefault(s => s?["if"]?.ToString() == "average_slope > 8");
            Assert.NotNull(slope8);
            Assert.Equal("0.8", slope8["multiply_by"]?.ToString());

            var slope12 = speed.FirstOrDefault(s => s?["if"]?.ToString() == "average_slope > 12");
            Assert.NotNull(slope12);
            Assert.Equal("0.6", slope12["multiply_by"]?.ToString());
        }

        [Fact]
        public void SlopeRule_AvoidSlopesTrue_WithSteepTolerance_AppliesModeratePenalties()
        {
            var profile = new UserProfile { AvoidSlopes = true, MaxSlope = 12.0 };
            var builder = new CustomModelBuilder();
            new SlopeFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            Assert.NotNull(priority);
            var maxSlopeRule = priority.FirstOrDefault(p => p?["if"]?.ToString() == "average_slope > 12");
            Assert.NotNull(maxSlopeRule);
            Assert.Equal("0.2", maxSlopeRule["multiply_by"]?.ToString());

            var moderateRule = priority.FirstOrDefault(p => p?["if"]?.ToString() == "average_slope > 7.2");
            Assert.NotNull(moderateRule);
            Assert.Equal("0.5", moderateRule["multiply_by"]?.ToString());
        }

        [Fact]
        public void DimensionsRule_LengthFallback_CalculatesEffectiveWidth()
        {
            var profile = new UserProfile { Length = 1.2 };
            var builder = new CustomModelBuilder();
            new DimensionsFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            // effectiveWidth = 1.2 * 0.75 = 0.9
            Assert.NotNull(priority);
            var widthBoundary = priority.FirstOrDefault(p => p?["if"]?.ToString() == "max_width < 0.9");
            Assert.NotNull(widthBoundary);
            Assert.Equal("0", widthBoundary["multiply_by"]?.ToString());
        }

        [Fact]
        public void DimensionsRule_ZeroOrNegativeWidth_Ignored()
        {
            var profile = new UserProfile { Width = 0.0 };
            var builder = new CustomModelBuilder();
            new DimensionsFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            var priority = model["priority"]?.AsArray();

            Assert.NotNull(priority);
            Assert.Empty(priority);
        }

        [Theory]
        [InlineData("blind", 3.5, 80)]
        [InlineData("wheelchair", 3.5, 70)]
        [InlineData("crutches", 3.0, 70)]
        [InlineData("walking", 5.0, 60)]
        public void SpeedAndDistanceRule_PresetDefaultsHandledCorrectly(string presetName, double expectedSpeed, int expectedDistance)
        {
            var json = JsonNode.Parse($"{{\"profile\": \"{presetName}\"}}");
            var profile = UserProfile.Parse(json);
            var builder = new CustomModelBuilder();
            new SpeedAndDistanceFeatureRule().Apply(profile, builder);

            var model = builder.Build();
            Assert.Equal(expectedDistance, model["distance_influence"]?.GetValue<int>());

            var speed = model["speed"]?.AsArray();
            Assert.NotNull(speed);
            var limitRule = speed.FirstOrDefault(s => s?["if"]?.ToString() == "true");
            Assert.NotNull(limitRule);
            Assert.Equal(expectedSpeed.ToString("0.#", CultureInfo.InvariantCulture), limitRule["limit_to"]?.ToString());
        }
    }

    public class HttpCallsUnitTests
    {
        [Theory]
        [InlineData("http://localhost:8989", "localhost:8989")]
        [InlineData("https://api.graphhopper.com", "api.graphhopper.com")]
        [InlineData("localhost:8989", "localhost:8989")]
        [InlineData("", "")]
        public void StripScheme_RemovesHttpAndHttpsPrefixes(string input, string expected)
        {
            var result = HttpCalls.StripScheme(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void JsonError_ProducesValidJsonWithErrorProperty()
        {
            var json = HttpCalls.JsonError("Connection failed");
            var node = JsonNode.Parse(json);

            Assert.NotNull(node);
            Assert.Equal("Connection failed", node["error"]?.ToString());
        }
    }

    public class ConcurrencyAndStressUnitTests
    {
        [Fact]
        public async Task ProfileFeatureRegistry_Default_ConcurrentExecutionIsThreadSafe()
        {
            var registry = ProfileFeatureRegistry.Default;
            var presets = new[] { "wheelchair", "blind", "crutches", "walking" };

            var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
            {
                var preset = presets[i % presets.Length];
                var profile = UserProfile.Parse(JsonNode.Parse($"{{\"profile\": \"{preset}\"}}"));
                var model = registry.BuildCustomModel(profile);

                Assert.NotNull(model["priority"]);
                Assert.NotNull(model["speed"]);
                Assert.NotNull(model["distance_influence"]);
            }));

            await Task.WhenAll(tasks);
        }
    }

    public class UserProfileParsingEdgeCasesUnitTests
    {
        [Fact]
        public void UserProfile_Parse_CaseInsensitivePropertyNames()
        {
            var json = JsonNode.Parse("{\"AVOID_STAIRS\": true, \"HAS_WHEELS\": \"YES\", \"MAX_SLOPE\": \"9.5\"}");
            var profile = UserProfile.Parse(json);

            Assert.False(profile.CanTakeStairs);
            Assert.True(profile.HasWheels);
            Assert.True(profile.AvoidSlopes);
            Assert.Equal(9.5, profile.MaxSlope);
        }

        [Fact]
        public void UserProfile_Parse_StringNumberConversions()
        {
            var json = JsonNode.Parse("{\"width\": \"0.85\", \"distance_influence\": \"85\", \"max_speed\": \"3.2\"}");
            var profile = UserProfile.Parse(json);

            Assert.Equal(0.85, profile.Width);
            Assert.Equal(85, profile.DistanceInfluence);
            Assert.Equal(3.2, profile.MaxSpeed);
        }

        [Fact]
        public void UserProfile_Parse_AvoidSlopesFalseOverridesMaxSlope()
        {
            var json = JsonNode.Parse("{\"max_slope\": 12.0, \"avoid_slopes\": false}");
            var profile = UserProfile.Parse(json);

            Assert.Equal(12.0, profile.MaxSlope);
            Assert.False(profile.AvoidSlopes);
        }

        [Fact]
        public void UserProfile_Parse_MaxSlopeSetsMaxSlopeAndAvoidSlopes()
        {
            var json = JsonNode.Parse("{\"max_slope\": 7.5}");
            var profile = UserProfile.Parse(json);

            Assert.Equal(7.5, profile.MaxSlope);
            Assert.True(profile.AvoidSlopes);
        }
    }

    public class LocaleParameterUnitTests
    {
        [Fact]
        public async Task AstreaRouteAsync_AcceptsCustomLocaleParameter()
        {
            var points = new List<PointResponse>();

            // Validation occurs before HTTP call, proving signature and invocation pass correctly
            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await AstreaEngine.AstreaEngine.AstreaRouteAsync(
                    "http://localhost:8989",
                    points,
                    "{\"profile\":\"foot\"}",
                    locale: "en"
                );
            });

            Assert.Contains("at least 2 points", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AstreaRoute_AcceptsCustomLocaleParameter()
        {
            var points = new List<PointResponse>();

            var exception = Assert.Throws<ValidationException>(() =>
            {
                AstreaEngine.AstreaEngine.AstreaRoute(
                    "http://localhost:8989",
                    points,
                    "{\"profile\":\"foot\"}",
                    locale: "de"
                );
            });

            Assert.Contains("at least 2 points", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class RouteResponseParsingTests
    {
        [Fact]
        public void ParseJson_ValidGraphHopperJson_ParsesAllFieldsCorrectly()
        {
            const string json = """
            {
                "paths": [
                    {
                        "distance": 850.4,
                        "time": 680000,
                        "ascend": 14.2,
                        "descend": 6.8,
                        "bbox": [-1.558, 47.214, -1.551, 47.219],
                        "points": {
                            "type": "LineString",
                            "coordinates": [
                                [-1.5536, 47.2172, 24.5],
                                [-1.5539, 47.2175]
                            ]
                        },
                        "instructions": [
                            {
                                "distance": 120.5,
                                "time": 96400,
                                "interval": [0, 1],
                                "sign": 0,
                                "text": "Continuer sur Rue de Strasbourg",
                                "street_name": "Rue de Strasbourg",
                                "heading": 84.5,
                                "last_heading": 85.0
                            },
                            {
                                "distance": 0.0,
                                "time": 0,
                                "interval": [1, 1],
                                "sign": 4,
                                "text": "Arrivée à destination",
                                "street_name": ""
                            }
                        ]
                    }
                ]
            }
            """;

            var result = Routes.ParseJson(json);

            Assert.NotNull(result);
            Assert.Equal(850.4, result.Distance);
            Assert.Equal(680000, result.Time);
            Assert.Equal(14.2, result.Ascend);
            Assert.Equal(6.8, result.Descend);
            Assert.NotNull(result.BBox);
            Assert.Equal(4, result.BBox.Length);
            Assert.Equal(-1.558, result.BBox[0]);
            Assert.Equal(47.214, result.BBox[1]);
            Assert.Equal(-1.551, result.BBox[2]);
            Assert.Equal(47.219, result.BBox[3]);

            Assert.Equal(2, result.Points.Count);
            Assert.Equal(47.2172, result.Points[0].Lat);
            Assert.Equal(-1.5536, result.Points[0].Lng);
            Assert.Equal(24.5, result.Points[0].Elevation);

            Assert.Equal(47.2175, result.Points[1].Lat);
            Assert.Equal(-1.5539, result.Points[1].Lng);
            Assert.Null(result.Points[1].Elevation);

            Assert.Equal(2, result.Instructions.Count);
            var firstInstruction = result.Instructions[0];
            Assert.Equal("Continuer sur Rue de Strasbourg", firstInstruction.Text);
            Assert.Equal("Rue de Strasbourg", firstInstruction.StreetName);
            Assert.Equal(120.5, firstInstruction.Distance);
            Assert.Equal(96400, firstInstruction.Time);
            Assert.Equal(0, firstInstruction.Sign);
            Assert.Equal(new[] { 0, 1 }, firstInstruction.Interval);
            Assert.Equal(84.5, firstInstruction.Heading);
            Assert.Equal(85.0, firstInstruction.LastHeading);

            var secondInstruction = result.Instructions[1];
            Assert.Equal("Arrivée à destination", secondInstruction.Text);
            Assert.Equal(4, secondInstruction.Sign);
            Assert.Null(secondInstruction.Heading);
        }

        [Fact]
        public void ParseJson_GraphHopperErrorPayload_ThrowsInvalidDataException()
        {
            const string json = "{\"message\": \"Point 0 is out of bounds: 100.0, 200.0\"}";

            var ex = Assert.Throws<InvalidDataException>(() => Routes.ParseJson(json));
            Assert.Contains("GraphHopper error: Point 0 is out of bounds", ex.Message);
        }

        [Fact]
        public void ParseJson_GraphHopperHintsErrorPayload_ThrowsInvalidDataException()
        {
            const string json = """
            {
                "hints": [
                    {
                        "message": "Cannot find point 0: 47.21, -1.55",
                        "details": "com.graphhopper.util.exceptions.PointNotFoundException"
                    }
                ]
            }
            """;

            var ex = Assert.Throws<InvalidDataException>(() => Routes.ParseJson(json));
            Assert.Contains("GraphHopper error: Cannot find point 0: 47.21, -1.55", ex.Message);
        }

        [Fact]
        public void ParseJson_GenericApiErrorPayload_ThrowsInvalidDataException()
        {
            const string json = "{\"error\": \"Internal Server Error\"}";

            var ex = Assert.Throws<InvalidDataException>(() => Routes.ParseJson(json));
            Assert.Contains("API error: Internal Server Error", ex.Message);
        }

        [Fact]
        public void ParseJson_MissingPaths_ThrowsInvalidDataException()
        {
            const string json = "{\"paths\": []}";

            var ex = Assert.Throws<InvalidDataException>(() => Routes.ParseJson(json));
            Assert.Contains("JSON response does not contain expected route structure", ex.Message);
        }

        [Fact]
        public void ParseJson_MalformedJson_ThrowsInvalidDataException()
        {
            const string json = "not valid json";

            var ex = Assert.Throws<InvalidDataException>(() => Routes.ParseJson(json));
            Assert.Contains("Failed to parse route response JSON", ex.Message);
        }
    }
}
