using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using AstreaEngine;

namespace AstreaEngineTests
{
    public class AstreaEngineUnitTests
    {
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
                    "http://localhost:8989",
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
                    "http://localhost:8989",
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
                    "http://localhost:8989",
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
                    "http://localhost:8989",
                    points,
                    "invalid json"
                );
            });

            Assert.Contains("Invalid user JSON", exception.Message);
        }
    }
}