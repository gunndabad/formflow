using System;
using System.Collections.Generic;
using FormFlow.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace FormFlow.Tests
{
    public class JourneyInstanceProviderTests
    {
        [Fact]
        public void CreateInstance_NoActionContext_ThrowsInvalidOperationException()
        {
            // Arrange
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var actionContextAccessor = Mock.Of<IActionContextAccessor>();

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.CreateInstance((object)state));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("No active ActionContext.", ex.Message);
        }

        [Fact]
        public void CreateInstance_ActionHasNoMetadata_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.CreateInstance((object)state));

            // Act & Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("No journey metadata found on action.", ex.Message);
        }

        [Fact]
        public void CreateInstance_StateTypeIsIncompatible_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();
            var descriptorStateType = typeof(OtherTestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, descriptorStateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.CreateInstance((object)state));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal($"{typeof(TestState).FullName} is not compatible with the journey's state type ({typeof(OtherTestState).FullName}).", ex.Message);
        }

        [Fact]
        public void CreateInstance_InstanceAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            var routeValues = new RouteValueDictionary()
            {
                { "id", 42 },
                { "subid", 69 }
            };
            var instanceId = new JourneyInstanceId(journeyName, routeValues);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty()));

            var httpContext = new DefaultHttpContext();
            httpContext.GetRouteData().Values.AddRange(routeValues);

            var routeData = new RouteData(routeValues);

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(
                    journeyName,
                    stateType,
                    dependentRouteDataKeys: new[] { "id", "subid" },
                    useRandomExtension: false));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.CreateInstance(state));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("Instance already exists with this ID.", ex.Message);
        }

        [Fact]
        public void CreateInstance_CreatesInstanceInStateStore()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new PropertiesBuilder()
                .Add("foo", 1)
                .Add("bar", 2)
                .Build();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.CreateInstance(
                    journeyName,
                    It.IsAny<JourneyInstanceId>(),  // FIXME
                    stateType,
                    state,
                    It.Is<IReadOnlyDictionary<object, object>>(d =>
                        d.Count == 2 && (int)d["foo"] == 1 && (int)d["bar"] == 2)))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties))
                .Verifiable();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.CreateInstance(state, properties);

            // Assert
            stateProvider.Verify();
            Assert.NotNull(result);
            Assert.Equal(journeyName, result.JourneyName);
            Assert.Equal(instanceId, result.InstanceId);
            Assert.Equal(stateType, result.StateType);
            Assert.Same(state, result.State);
            Assert.False(result.Completed);
            Assert.False(result.Deleted);
            Assert.Equal(2, result.Properties.Count);
            Assert.Equal(1, result.Properties["foo"]);
            Assert.Equal(2, result.Properties["bar"]);
        }

        [Fact]
        public void CreateInstanceOfT_StateTypeIsIncompatible_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            TestState state = new TestState();
            var descriptorStateType = typeof(OtherTestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, descriptorStateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.CreateInstance(state));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal($"{typeof(TestState).FullName} is not compatible with the journey's state type ({typeof(OtherTestState).FullName}).", ex.Message);
        }

        [Fact]
        public void GetInstance_NoActionContext_ThrowsInvalidOperationException()
        {
            // Arrange
            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var actionContextAccessor = Mock.Of<IActionContextAccessor>();

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.GetInstance());

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("No active ActionContext.", ex.Message);
        }

        [Fact]
        public void GetInstance_ActionHasNoMetadata_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.GetInstance());

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("No journey metadata found on action.", ex.Message);
        }

        [Fact]
        public void GetInstance_InstanceDoesNotExist_ReturnsNull()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new PropertiesBuilder()
                .Add("foo", 1)
                .Add("bar", 2)
                .Build();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns((JourneyInstance)null);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.GetInstance();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetInstance_InstanceDoesExist_ReturnsInstance()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new PropertiesBuilder()
                .Add("foo", 1)
                .Add("bar", 2)
                .Build();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.GetInstance();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(journeyName, result.JourneyName);
            Assert.Equal(instanceId, result.InstanceId);
            Assert.Equal(stateType, result.StateType);
            Assert.Same(state, result.State);
            Assert.False(result.Completed);
            Assert.False(result.Deleted);
            Assert.Equal(2, result.Properties.Count);
            Assert.Equal(1, result.Properties["foo"]);
            Assert.Equal(2, result.Properties["bar"]);
        }

        [Fact]
        public void GetInstanceOfT_StateTypeIsIncompatible_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new PropertiesBuilder()
                .Add("foo", 1)
                .Add("bar", 2)
                .Build();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.GetInstance<OtherTestState>());

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal($"{typeof(OtherTestState).FullName} is not compatible with the journey's state type ({typeof(TestState).FullName}).", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstance_NoActionContext_ThrowsInvalidOperationException()
        {
            // Arrange
            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var actionContextAccessor = Mock.Of<IActionContextAccessor>();

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.GetOrCreateInstance(() => new TestState()));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("No active ActionContext.", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstance_ActionHasNoMetadata_ThrowsInvalidOperationException()
        {
            // Arrange
            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.GetOrCreateInstance(() => new TestState()));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("No journey metadata found on action.", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstance_InstanceDoesNotExist_CreatesInstanceInStateStore()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new PropertiesBuilder()
                .Add("foo", 1)
                .Add("bar", 2)
                .Build();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns((JourneyInstance)null);
            stateProvider
                .Setup(mock => mock.CreateInstance(
                    journeyName,
                    It.IsAny<JourneyInstanceId>(),  // FIXME
                    stateType,
                     It.IsAny<object>(),
                    It.Is<IReadOnlyDictionary<object, object>>(d =>
                        d.Count == 2 && (int)d["foo"] == 1 && (int)d["bar"] == 2)))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties))
                .Verifiable();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.GetOrCreateInstance(() => new TestState(), properties);

            // Assert
            stateProvider.Verify();
            Assert.NotNull(result);
            Assert.Equal(journeyName, result.JourneyName);
            Assert.Equal(instanceId, result.InstanceId);
            Assert.Equal(stateType, result.StateType);
            Assert.Same(state, result.State);
            Assert.False(result.Completed);
            Assert.False(result.Deleted);
            Assert.Equal(2, result.Properties.Count);
            Assert.Equal(1, result.Properties["foo"]);
            Assert.Equal(2, result.Properties["bar"]);
        }

        [Fact]
        public void GetOrCreateInstance_CreateStateStateTypeIsIncompatible_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns((JourneyInstance)null);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.GetOrCreateInstance(() => (object)new OtherTestState()));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal($"{typeof(OtherTestState).FullName} is not compatible with the journey's state type ({typeof(TestState).FullName}).", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstance_InstanceDoesExist_ReturnsExistingInstance()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object originalState = new TestState();

            var properties = new PropertiesBuilder()
                .Add("foo", 1)
                .Add("bar", 2)
                .Build();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        originalState,
                        properties));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            var executedStateFactory = false;

            // Act
            var result = instanceProvider.GetOrCreateInstance(() =>
                {
                    executedStateFactory = true;
                    return new TestState();
                },
                properties);

            // Assert
            Assert.False(executedStateFactory);
            Assert.NotNull(result);
            Assert.Equal(journeyName, result.JourneyName);
            Assert.Equal(instanceId, result.InstanceId);
            Assert.Equal(stateType, result.StateType);
            Assert.Same(originalState, result.State);
            Assert.False(result.Completed);
            Assert.False(result.Deleted);
            Assert.Equal(2, result.Properties.Count);
            Assert.Equal(1, result.Properties["foo"]);
            Assert.Equal(2, result.Properties["bar"]);
        }

        [Fact]
        public void GetOrCreateInstanceOfT_CreateStateStateTypeIsIncompatible_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns((JourneyInstance)null);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(
                () => instanceProvider.GetOrCreateInstance(() => new OtherTestState()));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal($"{typeof(OtherTestState).FullName} is not compatible with the journey's state type ({typeof(TestState).FullName}).", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstanceFfT_RequestedStateTypeIsIncompatible_ThrowsInvalidOperationException()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new PropertiesBuilder()
                .Add("foo", 1)
                .Add("bar", 2)
                .Build();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var ex = Record.Exception(() => instanceProvider.GetOrCreateInstance(() => new OtherTestState()));

            // Assert
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal($"{typeof(OtherTestState).FullName} is not compatible with the journey's state type ({typeof(TestState).FullName}).", ex.Message);
        }

        [Fact]
        public void IsCurrentInstance_InstanceMatches_ReturnsTrue()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            var otherInstanceId = new JourneyInstanceId(
                journeyName,
                new RouteValueDictionary()
                {
                    { Constants.RandomExtensionQueryParameterName, randomExt }
                });

            // Act
            var result = instanceProvider.IsCurrentInstance(otherInstanceId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCurrentInstance_NoCurrentInstance_ReturnsFalse()
        {
            // Arrange
            var journeyName = "test-flow";
            CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            var otherInstanceId = new JourneyInstanceId("another-id", new RouteValueDictionary());

            // Act
            var result = instanceProvider.IsCurrentInstance(otherInstanceId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCurrentInstance_DifferentInstanceToCurrent_ReturnsFalse()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            var otherInstanceId = new JourneyInstanceId("another-id", new RouteValueDictionary());

            // Act
            var result = instanceProvider.IsCurrentInstance(otherInstanceId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryResolveExistingInstance_ActionHasNoMetadata_ReturnsNull()
        {
            // Arrange
            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            var actionDescriptor = new ActionDescriptor();

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        [Fact]
        public void TryResolveExistingInstance_CannotExtractIdForRouteValues_ReturnsNull()
        {
            // Arrange
            var journeyName = "test-flow";
            var routeValues = new RouteValueDictionary()
            {
                { "id", 42 },
                { "subid", 69 }
            };
            var instanceId = new JourneyInstanceId(journeyName, routeValues);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty()));

            var httpContext = new DefaultHttpContext();

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(
                    journeyName,
                    stateType,
                    dependentRouteDataKeys: new[] { "id", "subid" },
                    useRandomExtension: false));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        [Fact]
        public void TryResolveExistingInstance_CannotExtractIdForRandomId_ReturnsNull()
        {
            // Arrange
            var journeyName = "test-flow";
            var stateType = typeof(TestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        [Fact]
        public void TryResolveExistingInstance_InstanceDoesNotExistInStateStore_ReturnsNull()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns((JourneyInstance)null);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        [Fact]
        public void TryResolveExistingInstance_MismatchingJourneyNames_ReturnsNull()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();
            var descriptorJourneyName = "another-name";

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(descriptorJourneyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        [Fact]
        public void TryResolveExistingInstance_MismatchingStateType_ReturnsNull()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();
            var descriptorStateType = typeof(OtherTestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, descriptorStateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        [Fact]
        public void TryResolveExistingInstance_InstanceExistsForRandomId_ReturnsInstance()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.True(result);
            Assert.NotNull(instance);
            Assert.Equal(journeyName, instance.JourneyName);
            Assert.Equal(instanceId, instance.InstanceId);
            Assert.Equal(stateType, instance.StateType);
        }

        [Fact]
        public void TryResolveExistingInstance_InstanceExistsForRouteValues_ReturnsInstance()
        {
            // Arrange
            var journeyName = "test-flow";
            var routeValues = new RouteValueDictionary()
            {
                { "id", 42 },
                { "subid", 69 }
            };
            var instanceId = new JourneyInstanceId(journeyName, routeValues);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/foo/42/69";
            httpContext.GetRouteData().Values.AddRange(routeValues);

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(
                    journeyName,
                    stateType,
                    dependentRouteDataKeys: new[] { "id", "subid" },
                    useRandomExtension: false));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.True(result);
            Assert.NotNull(instance);
            Assert.Equal(journeyName, instance.JourneyName);
            Assert.Equal(instanceId, instance.InstanceId);
            Assert.Equal(stateType, instance.StateType);
        }

        [Fact]
        public void TryResolveExistingInstance_InstanceIsDeleted_ReturnsFalse()
        {
            // Arrange
            var journeyName = "test-flow";
            var instanceId = CreateIdWithRandomExtensionOnly(journeyName, out var randomExt);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(() =>
                {
                    var instance = JourneyInstance.Create(
                        stateProvider.Object,
                        journeyName,
                        instanceId,
                        stateType,
                        state,
                        properties: PropertiesBuilder.CreateEmpty());
                    instance.Deleted = true;
                    return instance;
                });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={randomExt}");

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new JourneyDescriptor(journeyName, stateType, Array.Empty<string>(), useRandomExtension: true));

            CreateActionContext(
                httpContext,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new JourneyInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        private static void CreateActionContext(
            HttpContext httpContext,
            ActionDescriptor actionDescriptor,
            out ActionContext actionContext,
            out IActionContextAccessor actionContextAccessor)
        {
            actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), actionDescriptor);

            var actionContextAccessorMock = new Mock<IActionContextAccessor>();
            actionContextAccessorMock.SetupGet(mock => mock.ActionContext).Returns(actionContext);

            actionContextAccessor = actionContextAccessorMock.Object;
        }

        private static JourneyInstanceId CreateIdWithRandomExtensionOnly(
            string journeyName,
            out string randomExt)
        {
            randomExt = Guid.NewGuid().ToString();

            return new JourneyInstanceId(
                journeyName,
                new RouteValueDictionary()
                {
                    { Constants.RandomExtensionQueryParameterName, randomExt }
                });
        }

        private class TestState { }

        private class OtherTestState { }
    }
}
