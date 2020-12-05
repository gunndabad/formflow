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
                requestDataKeys: new[] { "id" },
                appendUniqueKey: true);

            var httpContext = new DefaultHttpContext();

            // Act
            var ex = Record.Exception(() => JourneyInstanceId.Create(journeyDescriptor, httpContext.Request));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("Request is missing dependent route data entry: 'id'.", ex.Message);
        }

        [Fact]
        public void Create_NoDependentRouteDataKeysWithoutUniqueKey_ReturnsCorrectInstance()
        {
            CreateReturnsExpectedInstance(
                requestDataKeys: Array.Empty<string>(),
                useUniqueKey: false,
                requestQuery: null,
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 0,
                assertions: instanceId => { },
                expectedSerializedInstanceId: () => $"key");
        }

        [Fact]
        public void Create_NoDependentRouteDataKeysWithUniqueKey_ReturnsCorrectInstance()
        {
            string randomExt = default;

            CreateReturnsExpectedInstance(
                requestDataKeys: Array.Empty<string>(),
                useUniqueKey: true,
                requestQuery: null,
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.UniqueKeyQueryParameterName] as string;
                    Assert.NotNull(randomExt);
                },
                expectedSerializedInstanceId: () => $"key?ffiid={randomExt}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInRouteTemplateWithoutUniqueKey_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();

            CreateReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: false,
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
        public void Create_DependentRouteDataKeyInRouteTemplateWithUniqueKey_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();
            string randomExt = default;

            CreateReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: true,
                requestQuery: null,
                addRouteData: routeData =>
                {
                    routeData.Values.Add("id", id);
                },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.UniqueKeyQueryParameterName] as string;
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}&ffiid={randomExt}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInQueryStringWithoutUniqueKey_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();

            CreateReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: false,
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
        public void Create_DependentRouteDataKeyInQueryStringWithUniqueKey_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();
            string randomExt = default;

            CreateReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", id }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.UniqueKeyQueryParameterName] as string;
                    Assert.Equal(id, instanceId.RouteValues["id"]);
                },
                expectedSerializedInstanceId: () => $"key?id={id}&ffiid={randomExt}");
        }

        [Fact]
        public void Create_DependentRouteDataKeyInQueryStringWithMultipleValuesWithoutUniqueKey_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            CreateReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: false,
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
        public void Create_DependentRouteDataKeyInQueryStringWithMultipleValuesWithUniqueKey_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            string randomExt = default;

            CreateReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", new[] { id1, id2 } }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 2,
                assertions: instanceId =>
                {
                    randomExt = instanceId.RouteValues[Constants.UniqueKeyQueryParameterName] as string;
                    var ids = (Assert.IsAssignableFrom<IEnumerable<string>>(instanceId.RouteValues["id"])).ToList();
                    Assert.Equal(2, ids.Count);
                    Assert.Equal(id1, ids[0]);
                    Assert.Equal(id2, ids[1]);
                },
                expectedSerializedInstanceId: () => $"key?id={id1}&id={id2}&ffiid={randomExt}");
        }

        [Fact]
        public void Create_DependentRouteDataKeysInRouteTemplateAndQueryStringWithoutUniqueKey_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            CreateReturnsExpectedInstance(
                requestDataKeys: new[] { "id1", "id2" },
                useUniqueKey: false,
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
        public void Create_DependentRouteDataKeysInRouteTemplateAndQueryStringWithUniqueKey_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            string randomExt = default;

            CreateReturnsExpectedInstance(
                requestDataKeys: new[] { "id1", "id2" },
                useUniqueKey: true,
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
                    randomExt = instanceId.RouteValues[Constants.UniqueKeyQueryParameterName] as string;
                    Assert.Equal(id1, instanceId.RouteValues["id1"]);
                    Assert.Equal(id2, instanceId.RouteValues["id2"]);
                },
                expectedSerializedInstanceId: () => $"key?id1={id1}&id2={id2}&ffiid={randomExt}");
        }

        [Fact]
        public void Create_UniqueKeyAlreadyInRouteData_ReturnsInstanceWithNewUniqueKey()
        {
            var currentRandomExt = Guid.NewGuid().ToString();
            string newRandomExt = default;

            CreateReturnsExpectedInstance(
                requestDataKeys: Array.Empty<string>(),
                useUniqueKey: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { Constants.UniqueKeyQueryParameterName, currentRandomExt }
                }),
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 1,
                assertions: instanceId =>
                {
                    newRandomExt = instanceId.RouteValues[Constants.UniqueKeyQueryParameterName] as string;
                    Assert.NotNull(newRandomExt);
                    Assert.NotEqual(currentRandomExt, newRandomExt);
                },
                expectedSerializedInstanceId: () => $"key?ffiid={newRandomExt}");
        }

        private void CreateReturnsExpectedInstance(
            IEnumerable<string> requestDataKeys,
            bool useUniqueKey,
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
                requestDataKeys: requestDataKeys,
                appendUniqueKey: useUniqueKey);

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
                requestDataKeys: new[] { "id" },
                appendUniqueKey: false);

            var httpContext = new DefaultHttpContext();

            // Act
            var result = JourneyInstanceId.TryResolve(journeyDescriptor, httpContext.Request, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryResolve_MissingUniqueKey_ReturnsFalse()
        {
            // Arrange
            var journeyDescriptor = new JourneyDescriptor(
                journeyName: "key",
                stateType: typeof(State),
                requestDataKeys: new[] { "id" },
                appendUniqueKey: true);

            var httpContext = new DefaultHttpContext();
            httpContext.GetRouteData().Values.Add("id", Guid.NewGuid().ToString());

            // Act
            var result = JourneyInstanceId.TryResolve(journeyDescriptor, httpContext.Request, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryResolve_NoDependentRouteDataKeysWithoutUniqueKey_ReturnsCorrectInstance()
        {
            TryResolveReturnsExpectedInstance(
                requestDataKeys: Array.Empty<string>(),
                useUniqueKey: false,
                requestQuery: null,
                addRouteData: routeData => { },
                expectedInstanceRouteValueCount: 0,
                assertions: instanceId => { },
                expectedSerializedInstanceId: () => $"key");
        }

        [Fact]
        public void TryResolve_NoDependentRouteDataKeysWithUniqueKey_ReturnsCorrectInstance()
        {
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: Array.Empty<string>(),
                useUniqueKey: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { Constants.UniqueKeyQueryParameterName, randomExt }
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
        public void TryResolve_DependentRouteDataKeyInRouteTemplateWithoutUniqueKey_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: false,
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
        public void TryResolve_DependentRouteDataKeyInRouteTemplateWithUniqueKey_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { Constants.UniqueKeyQueryParameterName, randomExt }
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
        public void TryResolve_DependentRouteDataKeyInQueryStringWithoutUniqueKey_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: false,
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
        public void TryResolve_DependentRouteDataKeyInQueryStringWithUniqueKey_ReturnsCorrectInstance()
        {
            var id = Guid.NewGuid().ToString();
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", id },
                    { Constants.UniqueKeyQueryParameterName, randomExt }
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
        public void TryResolve_DependentRouteDataKeyInQueryStringWithMultipleValuesWithoutUniqueKey_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: false,
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
        public void TryResolve_DependentRouteDataKeyInQueryStringWithMultipleValuesWithUniqueKey_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: new[] { "id" },
                useUniqueKey: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id", new[] { id1, id2 } },
                    { Constants.UniqueKeyQueryParameterName, randomExt }
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
        public void TryResolve_DependentRouteDataKeysInRouteTemplateAndQueryStringWithoutUniqueKey_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: new[] { "id1", "id2" },
                useUniqueKey: false,
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
        public void TryResolve_DependentRouteDataKeysInRouteTemplateAndQueryStringWithUniqueKey_ReturnsCorrectInstance()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            var randomExt = Guid.NewGuid().ToString();

            TryResolveReturnsExpectedInstance(
                requestDataKeys: new[] { "id1", "id2" },
                useUniqueKey: true,
                requestQuery: new QueryCollection(new Dictionary<string, StringValues>()
                {
                    { "id2", id2 },
                    { Constants.UniqueKeyQueryParameterName, randomExt }
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
            IEnumerable<string> requestDataKeys,
            bool useUniqueKey,
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
                requestDataKeys: requestDataKeys,
                appendUniqueKey: useUniqueKey);

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
