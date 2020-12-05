using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace FormFlow.Tests
{
    public class JourneyInstanceIdTests
    {
        [Fact]
        public void Create_MissingDependentRouteDataKey_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyDescriptor = new JourneyDescriptor(
                journeyName: "key",
                stateType: typeof(State),
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: true);

            var httpContext = new DefaultHttpContext();

            // Act
            var ex = Record.Exception(() => JourneyInstanceId.Create(journeyDescriptor, httpContext.Request));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("Request is missing dependent route data entry: 'id'.", ex.Message);
        }

        [Fact]
        public void Create_NoDependentRouteDataKeysWithoutRandomExtension_ReturnsCorrectInstance()
        {
            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: Array.Empty<string>(),
                useRandomExtension: false,
                requestQuery: null,
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 0,
                assertions: instanceId => { },
                expectedSerializedInstanceId: () => $"key");
        }

        [Fact]
        public void Create_NoDependentRouteDataKeysWithRandomExtension_ReturnsCorrectInstance()
        {
            string randomExt = default;

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: Array.Empty<string>(),
                useRandomExtension: true,
                requestQuery: null,
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.RandomExtensionQueryParameterName] as string;
                    Assert.NotNull(randomExt);
                },
                expectedSerializedInstanceId: () => $"key?ffiid={randomExt}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInRouteTemplateWithoutRandomExtension_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: false,
                requestQuery: null,
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id", id);
                },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInRouteTemplateWithRandomExtension_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();
            string randomExt = default;

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: true,
                requestQuery: null,
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id", id);
                },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.RandomExtensionQueryParameterName] as string;
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}&ffiid={randomExt}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInQueryStringWithoutRandomExtension_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: false,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", id }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInQueryStringWithRandomExtension_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();
            string randomExt = default;

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", id }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.RandomExtensionQueryParameterName] as string;
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}&ffiid={randomExt}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInQueryStringWithMultipleValuesWithoutRandomExtension_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: false,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", new[] { id1, id2 } }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    var ids = (Assert.IsAssignableFrom<IEnumerable<string>>(instanceId.RouteValues["id"])).ToList();
                    Assert.Equal(2, ids.Count);
                    Assert.Equal(id1, ids[0]);
                    Assert.Equal(id2, ids[1]);
                },
                expectedSerializedInstanceId: () => $"key?id={id1}&id={id2}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInQueryStringWithMultipleValuesWithRandomExtension_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            string randomExt = default;

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", new[] { id1, id2 } }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.RandomExtensionQueryParameterName] as string;
                    var ids = (Assert.IsAssignableFrom<IEnumerable<string>>(instanceId.RouteValues["id"])).ToList();
                    Assert.Equal(2, ids.Count);
                    Assert.Equal(id1, ids[0]);
                    Assert.Equal(id2, ids[1]);
                },
                expectedSerializedInstanceId: () => $"key?id={id1}&id={id2}&ffiid={randomExt}");
        }

        [Fact]
        public void Create_DependentRouteDataKeysInRouteTemplateAndQueryStringWithoutRandomExtension_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id1", "id2" },
                useRandomExtension: false,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id2", id2 }
                }),
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id1", id1);
                },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    Assert.Equal(id1, instanceId.RouteValues["id1"]);
                    Assert.Equal(id2, instanceId.RouteValues["id2"]);
                },
                expectedSerializedInstanceId: () => $"key?id1={id1}&id2={id2}");
        }

        [Fact]
        public void Create_DependentRouteDataKeysInRouteTemplateAndQueryStringWithRandomExtension_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            string randomExt = default;

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id1", "id2" },
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id2", id2 }
                }),
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id1", id1);
                },
                expectedInstanceRouteValueCount: 3,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.RandomExtensionQueryParameterName] as string;
                    Assert.Equal(id1, instanceId.RouteValues["id1"]);
                    Assert.Equal(id2, instanceId.RouteValues["id2"]);
                },
                expectedSerializedInstanceId: () => $"key?id1={id1}&id2={id2}&ffiid={randomExt}");
        }

        [Fact]
        public void Create_RandomExtensionAlreadyInRouteData_ReturnsInstanceWithNewRandomExtension()
        {
            var currentRandomExt = Guid.NewGuid().ToString();
            string newRandomExt = default;

            CreateReturnsExpectedInstance(
                dependentRouteDataKeys: Array.Empty<string>(),
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { Constants.RandomExtensionQueryParameterName, currentRandomExt }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    newRandomExt = instanceId.RouteValues[Constants.RandomExtensionQueryParameterName] as string;
                    Assert.NotNull(newRandomExt);
                    Assert.NotEqual(currentRandomExt, newRandomExt);
                },
                expectedSerializedInstanceId: () => $"key?ffiid={newRandomExt}");
        }

        private void CreateReturnsExpectedInstance(
            IEnumerable<string> dependentRouteDataKeys,
            bool useRandomExtension,
            IQueryCollection requestQuery,
            Action<RouteData> addRouteData,
            int expectedInstanceRouteValueCount,
            Action<JourneyInstanceId> assertions,
            Func<string> expectedSerializedInstanceId)
        {
            // Arrange
            var journeyDescriptor = new JourneyDescriptor(
                journeyName: "key",
                stateType: typeof(State),
                dependentRouteDataKeys: dependentRouteDataKeys,
                useRandomExtension: useRandomExtension);

            var id = Guid.NewGuid().ToString();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Query = requestQuery ?? new QueryCollection();
            addRouteData?.Invoke(httpContext.GetRouteData());

            // Act
            var instanceId = JourneyInstanceId.Create(journeyDescriptor, httpContext.Request);

            // Assert
            Assert.Equal("key", instanceId.JourneyName);
            Assert.Equal(expectedInstanceRouteValueCount, instanceId.RouteValues.Count);
            assertions(instanceId);
            Assert.Equal(expectedSerializedInstanceId(), instanceId.ToString());
        }

        [Fact]
        public void TryResolve_MissingDependentRouteDataKey_ReturnsFalse()
        {
            // Arrange
            var journeyDescriptor = new JourneyDescriptor(
                journeyName: "key",
                stateType: typeof(State),
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: false);

            var httpContext = new DefaultHttpContext();

            // Act
            var result = JourneyInstanceId.TryResolve(journeyDescriptor, httpContext.Request, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryResolve_MissingRandomExtension_ReturnsFalse()
        {
            // Arrange
            var journeyDescriptor = new JourneyDescriptor(
                journeyName: "key",
                stateType: typeof(State),
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: true);

            var httpContext = new DefaultHttpContext();
            httpContext.GetRouteData().Values.Add("id", Guid.NewGuid().ToString());

            // Act
            var result = JourneyInstanceId.TryResolve(journeyDescriptor, httpContext.Request, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryResolve_NoDependentRouteDataKeysWithoutRandomExtension_ReturnsCorrectInstance()
        {
            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: Array.Empty<string>(),
                useRandomExtension: false,
                requestQuery: null,
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 0,
                assertions: instanceId => { },
                expectedSerializedInstanceId: () => $"key");
        }

        [Fact]
        public void TryResolve_NoDependentRouteDataKeysWithRandomExtension_ReturnsCorrectInstance()
        {
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: Array.Empty<string>(),
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { Constants.RandomExtensionQueryParameterName, randomExt }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    Assert.NotNull(randomExt);
                },
                expectedSerializedInstanceId: () => $"key?ffiid={randomExt}");
        }

        [Fact]
        public void TryResolve_DependentRouteDataKeyInRouteTemplateWithoutRandomExtension_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: false,
                requestQuery: null,
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id", id);
                },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}");
        }

        [Fact]
        public void TryResolve_DependentRouteDataKeyInRouteTemplateWithRandomExtension_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { Constants.RandomExtensionQueryParameterName, randomExt }
                }),
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id", id);
                },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}&ffiid={randomExt}");
        }

        [Fact]
        public void TryResolve_DependentRouteDataKeyInQueryStringWithoutRandomExtension_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: false,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", id }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}");
        }

        [Fact]
        public void TryResolve_DependentRouteDataKeyInQueryStringWithRandomExtension_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", id },
                    { Constants.RandomExtensionQueryParameterName, randomExt }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}&ffiid={randomExt}");
        }

        [Fact]
        public void TryResolve_DependentRouteDataKeyInQueryStringWithMultipleValuesWithoutRandomExtension_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: false,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", new[] { id1, id2 } }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    var ids = (Assert.IsAssignableFrom<IEnumerable<string>>(instanceId.RouteValues["id"])).ToList();
                    Assert.Equal(2, ids.Count);
                    Assert.Equal(id1, ids[0]);
                    Assert.Equal(id2, ids[1]);
                },
                expectedSerializedInstanceId: () => $"key?id={id1}&id={id2}");
        }

        [Fact]
        public void TryResolve_DependentRouteDataKeyInQueryStringWithMultipleValuesWithRandomExtension_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id" },
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", new[] { id1, id2 } },
                    { Constants.RandomExtensionQueryParameterName, randomExt }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    var ids = (Assert.IsAssignableFrom<IEnumerable<string>>(instanceId.RouteValues["id"])).ToList();
                    Assert.Equal(2, ids.Count);
                    Assert.Equal(id1, ids[0]);
                    Assert.Equal(id2, ids[1]);
                },
                expectedSerializedInstanceId: () => $"key?id={id1}&id={id2}&ffiid={randomExt}");
        }

        [Fact]
        public void TryResolve_DependentRouteDataKeysInRouteTemplateAndQueryStringWithoutRandomExtension_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id1", "id2" },
                useRandomExtension: false,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id2", id2 }
                }),
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id1", id1);
                },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    Assert.Equal(id1, instanceId.RouteValues["id1"]);
                    Assert.Equal(id2, instanceId.RouteValues["id2"]);
                },
                expectedSerializedInstanceId: () => $"key?id1={id1}&id2={id2}");
        }

        [Fact]
        public void TryResolve_DependentRouteDataKeysInRouteTemplateAndQueryStringWithRandomExtension_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                dependentRouteDataKeys: new[] { "id1", "id2" },
                useRandomExtension: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id2", id2 },
                    { Constants.RandomExtensionQueryParameterName, randomExt }
                }),
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id1", id1);
                },
                expectedInstanceRouteValueCount: 3,
                assertions: instanceId =>
                {
                    Assert.Equal(id1, instanceId.RouteValues["id1"]);
                    Assert.Equal(id2, instanceId.RouteValues["id2"]);
                },
                expectedSerializedInstanceId: () => $"key?id1={id1}&id2={id2}&ffiid={randomExt}");
        }

        private void TryResolveReturnsExpectedInstance(
            IEnumerable<string> dependentRouteDataKeys,
            bool useRandomExtension,
            IQueryCollection requestQuery,
            Action<RouteData> addRouteData,
            int expectedInstanceRouteValueCount,
            Action<JourneyInstanceId> assertions,
            Func<string> expectedSerializedInstanceId)
        {
            // Arrange
            var journeyDescriptor = new JourneyDescriptor(
                journeyName: "key",
                stateType: typeof(State),
                dependentRouteDataKeys: dependentRouteDataKeys,
                useRandomExtension: useRandomExtension);

            var id = Guid.NewGuid().ToString();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Query = requestQuery ?? new QueryCollection();
            addRouteData?.Invoke(httpContext.GetRouteData());

            // Act
            var result = JourneyInstanceId.TryResolve(journeyDescriptor, httpContext.Request, out var instanceId);

            // Assert
            Assert.True(result);
            Assert.Equal("key", instanceId.JourneyName);
            Assert.Equal(expectedInstanceRouteValueCount, instanceId.RouteValues.Count);
            assertions(instanceId);
            Assert.Equal(expectedSerializedInstanceId(), instanceId.ToString());
        }

        private class State { }
    }
}
