using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FormFlow.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FormFlow.Tests
{
    public class EndToEndTests : MvcTestBase
    {
        public EndToEndTests(MvcTestFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async Task ReadState_ReturnsStateAndProperties()
        {
            // Arrange
            var instance = StateProvider.CreateInstance(
                key: "RouteValuesE2ETests",
                instanceId: GenerateInstanceId(out var id, out var subid),
                stateType: typeof(RouteValuesE2ETestsState),
                state: RouteValuesE2ETestsState.CreateInitialState(),
                properties: new PropertiesBuilder().Add("bar", 42).Build());

            // Act & Assert
            var responseJson = await ReadStateAndAssert(instance.InstanceId, expectedValue: "initial");
            Assert.Equal(42, responseJson["properties"]["bar"]);
        }

        [Fact]
        public async Task UpdateState_UpdatesStateAndRedirects()
        {
            // Arrange
            var instance = StateProvider.CreateInstance(
                key: "RouteValuesE2ETests",
                instanceId: GenerateInstanceId(out var id, out var subid),
                stateType: typeof(RouteValuesE2ETestsState),
                state: RouteValuesE2ETestsState.CreateInitialState(),
                properties: new PropertiesBuilder().Add("bar", 42).Build());

            // Act & Assert
            await UpdateState(instance.InstanceId, newValue: "updated");
        }

        [Fact]
        public async Task Complete_DoesNotAllowStateToBeUpdatedSubsequently()
        {
            // Arrange
            var instance = StateProvider.CreateInstance(
                key: "RouteValuesE2ETests",
                instanceId: GenerateInstanceId(out var id, out var subid),
                stateType: typeof(RouteValuesE2ETestsState),
                state: RouteValuesE2ETestsState.CreateInitialState(),
                properties: new PropertiesBuilder().Add("bar", 42).Build());

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/RouteValuesE2ETests/{id}/{subid}/Complete")
            {
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await Assert.ThrowsAnyAsync<Exception>(() => UpdateState(instance.InstanceId, newValue: "anything"));
        }

        [Fact]
        public async Task Complete_DoesAllowStateToBeReadSubsequently()
        {
            // Arrange
            var instance = StateProvider.CreateInstance(
                key: "RouteValuesE2ETests",
                instanceId: GenerateInstanceId(out var id, out var subid),
                stateType: typeof(RouteValuesE2ETestsState),
                state: RouteValuesE2ETestsState.CreateInitialState(),
                properties: new PropertiesBuilder().Add("bar", 42).Build());

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/RouteValuesE2ETests/{id}/{subid}/Complete")
            {
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await ReadStateAndAssert(instance.InstanceId, expectedValue: "initial");
        }

        [Fact]
        public async Task Delete_ReturnsOk()
        {
            // Arrange
            var instance = StateProvider.CreateInstance(
                key: "RouteValuesE2ETests",
                instanceId: GenerateInstanceId(out var id, out var subid),
                stateType: typeof(RouteValuesE2ETestsState),
                state: RouteValuesE2ETestsState.CreateInitialState(),
                properties: new PropertiesBuilder().Add("bar", 42).Build());

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/RouteValuesE2ETests/{id}/{subid}/Delete")
            {
            };

            // Act & Assert
            var response = await HttpClient.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static FormFlowInstanceId GenerateInstanceId(out string id, out string subid)
        {
            id = Guid.NewGuid().ToString();
            subid = Guid.NewGuid().ToString();

            return new FormFlowInstanceId(
                key: "RouteValuesE2ETests",
                new RouteValueDictionary(
                    new Dictionary<string, object>()
                    {
                        { "id", id },
                        { "subid", subid }
                    }));
        }

        private async Task<JObject> ReadStateAndAssert(
            FormFlowInstanceId instanceId,
            string expectedValue)
        {
            var id = instanceId.RouteValues["id"];
            var subid = instanceId.RouteValues["id"];

            var response = await HttpClient.GetAsync(
                $"/RouteValuesE2ETests/{id}/{subid}/ReadState");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseObj = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(expectedValue, responseObj["state"]["value"]);

            return responseObj;
        }

        private async Task UpdateState(FormFlowInstanceId instanceId, string newValue)
        {
            var id = instanceId.RouteValues["id"];
            var subid = instanceId.RouteValues["subid"];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/RouteValuesE2ETests/{id}/{subid}/UpdateState")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "newValue", newValue }
                })
            };

            var response = await HttpClient.SendAsync(request);

            if ((int)response.StatusCode >= 400)
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }

    [Route("RouteValuesE2ETests/{id}/{subid}")]
    [FlowAction(key: "RouteValuesE2ETests", stateType: typeof(RouteValuesE2ETestsState), idRouteDataKeys: new[] { "id", "subid" }, useRandomExtension: false)]
    public class RouteValuesE2ETestsController : Controller
    {
        private readonly FormFlowInstanceProvider _formFlowInstanceProvider;

        public RouteValuesE2ETestsController(FormFlowInstanceProvider formFlowInstanceProvider)
        {
            _formFlowInstanceProvider = formFlowInstanceProvider;
        }

        [HttpGet("ReadState")]
        [RequireFormFlowInstance]
        public IActionResult ReadState()
        {
            var instance = _formFlowInstanceProvider.GetInstance<RouteValuesE2ETestsState>();

            return Json(new
            {
                Properties = instance.Properties,
                State = instance.State
            });
        }

        [HttpPost("UpdateState")]
        [RequireFormFlowInstance]
        public IActionResult UpdateState(string newValue)
        {
            var instance = _formFlowInstanceProvider.GetInstance<RouteValuesE2ETestsState>();
            instance.UpdateState(state => state.Value = newValue);
            return RedirectToAction(nameof(ReadState)).WithFormFlowInstance(instance);
        }

        [HttpPost("Complete")]
        [RequireFormFlowInstance]
        public IActionResult Complete()
        {
            var instance = _formFlowInstanceProvider.GetInstance<RouteValuesE2ETestsState>();
            instance.Complete();
            return Ok();
        }

        [HttpPost("Delete")]
        [RequireFormFlowInstance]
        public IActionResult Delete()
        {
            var instance = _formFlowInstanceProvider.GetInstance<RouteValuesE2ETestsState>();
            instance.Delete();
            return Ok();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _formFlowInstanceProvider.GetOrCreateInstance(
                RouteValuesE2ETestsState.CreateInitialState,
                properties: new PropertiesBuilder().Add("bar", 42).Build());
        }
    }

    public class RouteValuesE2ETestsState
    {
        public string Value { get; set; }

        public static RouteValuesE2ETestsState CreateInitialState() => new RouteValuesE2ETestsState()
        {
            Value = "initial"
        };
    }
}
