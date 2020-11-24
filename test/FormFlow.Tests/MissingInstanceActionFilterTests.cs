using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace FormFlow.Tests
{
    public class MissingInstanceActionFilterTests : MvcTestBase
    {
        public MissingInstanceActionFilterTests(MvcTestFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async Task RequireFormFlowInstanceSpecifiedButNoActiveInstance_ReturnsNotFound()
        {
            // Arrange
            var id = 42;
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"MissingInstanceActionFilterTests/{id}/withattribute");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RequireFormFlowInstanceSpecifiedWithActiveInstance_ReturnsOk()
        {
            // Arrange
            CreateInstanceForRouteParameters(
                key: "MissingInstanceActionFilterTests",
                routeParameters: new Dictionary<string, object>()
                {
                    { "id", "42" }
                },
                state: new MissingInstanceActionFilterTestsState());

            var id = 42;
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"MissingInstanceActionFilterTests/{id}/withattribute");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RequireFormFlowInstanceSpecifiedButNoMetadata_Throws()
        {
            // Arrange
            var id = 42;
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"MissingInstanceActionFilterTests/{id}/withoutmetadata");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => HttpClient.SendAsync(request));
            Assert.Equal("No FormFlow metadata found on action.", ex.Message);
        }
    }

    [Route("MissingInstanceActionFilterTests/{id}")]
    public class MissingInstanceActionFilterTestsController : Controller
    {
        [FormFlowAction(
            key: "MissingInstanceActionFilterTests",
            stateType: typeof(MissingInstanceActionFilterTestsState),
            idRouteParameterNames: "id")]
        [RequireFormFlowInstance]
        [HttpGet("withattribute")]
        public IActionResult WithAttribute() => Ok();

        [RequireFormFlowInstance]
        [HttpGet("withoutmetadata")]
        public IActionResult WithoutMetadata() => Ok();
    }

    public class MissingInstanceActionFilterTestsState { }
}
