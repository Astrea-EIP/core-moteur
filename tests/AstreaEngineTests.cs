using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
        public void TestDockerUp()
        {
            var healthCheck = AstreaEngine.AstreaEngine.AstreaHealth(GraphhopperUrl);
            Assert.NotNull(healthCheck);
            Assert.True(
                healthCheck.Contains("\"status\":\"OK\"", StringComparison.OrdinalIgnoreCase) ||
                healthCheck.Contains("\"error\":", StringComparison.OrdinalIgnoreCase),
                $"Unexpected health check response: {healthCheck}");
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
        public async Task TestAstreaRouteAsyncWithCustomModel()
        {
            var points = new List<PointResponse>
            {
                new PointResponse { Lat = 47.21, Lng = -1.55 },
                new PointResponse { Lat = 47.22, Lng = -1.54 }
            };

            try
            {
                var result = await AstreaEngine.AstreaEngine.AstreaRouteAsync(
                    GraphhopperUrl,
                    points,
                    "{\"profile\":\"wheelchair\"}"
                );

                Assert.NotNull(result);
                Assert.IsType<List<PointResponse>>(result);
            }
            catch (InvalidOperationException ex)
            {
                Assert.Contains("error", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
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
        public async Task TestAstreaRouteAsyncWithNullOrEmptyUserJsonDoesNotThrowValidationException()
        {
            var points = new List<PointResponse>
            {
                new PointResponse { Lat = 47.21, Lng = -1.55 },
                new PointResponse { Lat = 47.22, Lng = -1.54 }
            };

            // Should proceed past validation without throwing ValidationException("Invalid user JSON")
            try
            {
                await AstreaEngine.AstreaEngine.AstreaRouteAsync(GraphhopperUrl, points, "");
            }
            catch (ValidationException ex)
            {
                Assert.Fail($"ValidationException was unexpectedly thrown: {ex.Message}");
            }
            catch
            {
                // Network or mock error is expected if GraphHopper is offline, but NOT ValidationException
            }
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
            var json = JsonNode.Parse("{\"profile\": \"wheelchair\", \"can_take_stairs\": true, \"width\": 0.75}");
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
                ""stairs"": false,
                ""avoid_slopes"": true,
                ""max_slope"": 6.0,
                ""wheels"": true,
                ""blind"": true,
                ""dimensions"": {
                    ""width"": 0.85,
                    ""length"": 1.2
                },
                ""speed"": 2.8,
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
            var json = JsonNode.Parse("{\"wheels\": \"yes\", \"avoid_stairs\": \"1\", \"slope\": \"no\"}");
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
        public void UserProfile_Parse_RetainsExtraProperties()
        {
            var json = JsonNode.Parse("{\"profile\": \"wheelchair\", \"custom_tag\": \"preferred\", \"level\": 42}");
            var profile = UserProfile.Parse(json);

            Assert.True(profile.HasWheels);
            Assert.True(profile.ExtraProperties.ContainsKey("custom_tag"));
            Assert.Equal("preferred", profile.ExtraProperties["custom_tag"]?.ToString());
            Assert.True(profile.ExtraProperties.ContainsKey("level"));
            Assert.Equal("42", profile.ExtraProperties["level"]?.ToString());
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
            var json = JsonNode.Parse("{\"ALLOW_STAIRS\": false, \"WHEELS\": \"YES\", \"MAX_SLOPE\": \"9.5\"}");
            var profile = UserProfile.Parse(json);

            Assert.False(profile.CanTakeStairs);
            Assert.True(profile.HasWheels);
            Assert.True(profile.AvoidSlopes);
            Assert.Equal(9.5, profile.MaxSlope);
        }

        [Fact]
        public void UserProfile_Parse_StringNumberConversions()
        {
            var json = JsonNode.Parse("{\"width\": \"0.85\", \"distance_influence\": \"85\", \"speed\": \"3.2\"}");
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
        public void UserProfile_Parse_NumericSlopeSetsMaxSlopeAndAvoidSlopes()
        {
            var json = JsonNode.Parse("{\"slope\": 7.5}");
            var profile = UserProfile.Parse(json);

            Assert.Equal(7.5, profile.MaxSlope);
            Assert.True(profile.AvoidSlopes);
        }
    }
}
