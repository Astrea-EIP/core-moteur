using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using AstreaEngine;

namespace AstreaEngineTests
{
    public class AstreaEngineUnitTests
    {
        private bool IsErrorJson(string result)
        {
            try
            {
                var json = JsonNode.Parse(result);
                return json?["error"] != null;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidJson(string result)
        {
            try
            {
                JsonNode.Parse(result);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [Fact]
        public void TestAstreaRouteWithInvalidPoints()
        {
            var result = AstreaEngine.AstreaEngine.AstreaRoute(
                "http://localhost:8989",
                "47.21,-1.55",
                "{\"profile\":\"foot\"}"
            );

            Assert.True(IsValidJson(result), "Result should be valid JSON");
            Assert.True(IsErrorJson(result), "Result should contain an error field");
            Assert.Contains("at least 2 points", result);
        }

        [Fact]
        public async Task TestAstreaRouteAsyncWithInvalidPoints()
        {
            var result = await AstreaEngine.AstreaEngine.AstreaRouteAsync(
                "http://localhost:8989",
                "",
                "{\"profile\":\"foot\"}"
            );

            Assert.True(IsValidJson(result), "Result should be valid JSON");
            Assert.True(IsErrorJson(result), "Result should contain an error field");
            Assert.Contains("at least 2 points", result);
        }
    }
}