using System;
using System.Collections.Generic;
using FormFlow.Metadata;
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
    public class FormFlowInstanceProviderTests
    {
        [Fact]
        public void CreateInstance_NoActionContext_ThrowsInvalidOperationException()
        {
            // Arrange
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var actionContextAccessor = Mock.Of<IActionContextAccessor>();

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => instanceProvider.CreateInstance((object)state));
            Assert.Equal("No active ActionContext.", ex.Message);
        }

        [Fact]
        public void CreateInstance_ActionHasNoMetadata_ThrowsInvalidOperationException()
        {
            // Arrange
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => instanceProvider.CreateInstance((object)state));
            Assert.Equal("No FormFlow metadata found on action.", ex.Message);
        }

        [Fact]
        public void CreateInstance_StateTypeIsIncompatible_ThrowsInvalidOperationException()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            var state = new TestState();
            var descriptorStateType = typeof(OtherTestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, descriptorStateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => instanceProvider.CreateInstance((object)state));
            Assert.Equal($"{typeof(TestState).FullName} is not compatible with the FormFlow metadata's state type ({typeof(OtherTestState).FullName}).", ex.Message);
        }

        [Fact]
        public void CreateInstance_InstanceAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var key = "test-flow";
            var routeValues = new RouteValueDictionary()
            {
                { "id", 42 },
                { "subid", 69 }
            };
            var instanceId = FormFlowInstanceId.GenerateForRouteValues(key, routeValues);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties: new Dictionary<object, object>()));

            var httpContext = new DefaultHttpContext();

            var routeData = new RouteData(routeValues);

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new FormFlowDescriptor(
                    key,
                    stateType,
                    IdGenerationSource.RouteValues,
                    idRouteParameterNames: new[] { "id", "subid" }));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => instanceProvider.CreateInstance(state));
            Assert.Equal("Instance already exists with this ID.", ex.Message);
        }

        [Fact]
        public void CreateInstance_CreatesInstanceInStateStore()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new Dictionary<object, object>()
            {
                { "foo", 1 },
                { "bar", 2 }
            };

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.CreateInstance(
                    key,
                    It.IsAny<FormFlowInstanceId>(),  // FIXME
                    stateType,
                    state,
                    It.Is<IReadOnlyDictionary<object, object>>(d =>
                        d.Count == 2 && (int)d["foo"] == 1 && (int)d["bar"] == 2)))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties))
                .Verifiable();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.CreateInstance(state, properties);

            // Assert
            stateProvider.Verify();
            Assert.NotNull(result);
            Assert.Equal(key, result.Key);
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
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            TestState state = new TestState();
            var descriptorStateType = typeof(OtherTestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, descriptorStateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => instanceProvider.CreateInstance(state));
            Assert.Equal($"{typeof(TestState).FullName} is not compatible with the FormFlow metadata's state type ({typeof(OtherTestState).FullName}).", ex.Message);
        }

        [Fact]
        public void GetInstance_NoActionContext_ThrowsInvalidOperationException()
        {
            // Arrange
            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var actionContextAccessor = Mock.Of<IActionContextAccessor>();

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => instanceProvider.GetInstance());
            Assert.Equal("No active ActionContext.", ex.Message);
        }

        [Fact]
        public void GetInstance_ActionHasNoMetadata_ThrowsInvalidOperationException()
        {
            // Arrange
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => instanceProvider.GetInstance());
            Assert.Equal("No FormFlow metadata found on action.", ex.Message);
        }

        [Fact]
        public void GetInstance_InstanceDoesNotExist_ReturnsNull()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new Dictionary<object, object>()
            {
                { "foo", 1 },
                { "bar", 2 }
            };

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns((FormFlowInstance)null);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.GetInstance();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetInstance_InstanceDoesExist_ReturnsInstance()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new Dictionary<object, object>()
            {
                { "foo", 1 },
                { "bar", 2 }
            };

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.GetInstance();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(key, result.Key);
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
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new Dictionary<object, object>()
            {
                { "foo", 1 },
                { "bar", 2 }
            };

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => instanceProvider.GetInstance<OtherTestState>());
            Assert.Equal($"{typeof(OtherTestState).FullName} is not compatible with the FormFlow metadata's state type ({typeof(TestState).FullName}).", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstance_NoActionContext_ThrowsInvalidOperationException()
        {
            // Arrange
            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var actionContextAccessor = Mock.Of<IActionContextAccessor>();

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => instanceProvider.GetOrCreateInstance(() => new TestState()));
            Assert.Equal("No active ActionContext.", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstance_ActionHasNoMetadata_ThrowsInvalidOperationException()
        {
            // Arrange
            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var actionContextAccessor = Mock.Of<IActionContextAccessor>();

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => instanceProvider.GetOrCreateInstance(() => new TestState()));
            Assert.Equal("No active ActionContext.", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstance_InstanceDoesNotExist_CreatesInstanceInStateStore()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new Dictionary<object, object>()
            {
                { "foo", 1 },
                { "bar", 2 }
            };

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns((FormFlowInstance)null);
            stateProvider
                .Setup(mock => mock.CreateInstance(
                    key,
                    It.IsAny<FormFlowInstanceId>(),  // FIXME
                    stateType,
                     It.IsAny<object>(),
                    It.Is<IReadOnlyDictionary<object, object>>(d =>
                        d.Count == 2 && (int)d["foo"] == 1 && (int)d["bar"] == 2)))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties))
                .Verifiable();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.GetOrCreateInstance(() => new TestState(), properties);

            // Assert
            stateProvider.Verify();
            Assert.NotNull(result);
            Assert.Equal(key, result.Key);
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
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns((FormFlowInstance)null);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => instanceProvider.GetOrCreateInstance(() => (object)new OtherTestState()));
            Assert.Equal($"{typeof(OtherTestState).FullName} is not compatible with the FormFlow metadata's state type ({typeof(TestState).FullName}).", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstance_InstanceDoesExist_ReturnsExistingInstance()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object originalState = new TestState();

            var properties = new Dictionary<object, object>()
            {
                { "foo", 1 },
                { "bar", 2 }
            };

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        originalState,
                        properties));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

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
            Assert.Equal(key, result.Key);
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
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns((FormFlowInstance)null);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => instanceProvider.GetOrCreateInstance<OtherTestState>(() => new OtherTestState()));
            Assert.Equal($"{typeof(OtherTestState).FullName} is not compatible with the FormFlow metadata's state type ({typeof(TestState).FullName}).", ex.Message);
        }

        [Fact]
        public void GetOrCreateInstanceFfT_RequestedStateTypeIsIncompatible_ThrowsInvalidOperationException()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            object state = new TestState();

            var properties = new Dictionary<object, object>()
            {
                { "foo", 1 },
                { "bar", 2 }
            };

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(mock => mock.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => instanceProvider.GetOrCreateInstance(() => new OtherTestState()));
            Assert.Equal($"{typeof(OtherTestState).FullName} is not compatible with the FormFlow metadata's state type ({typeof(TestState).FullName}).", ex.Message);
        }

        [Fact]
        public void TryResolveExistingInstance_ActionHasNoMetadata_ReturnsNull()
        {
            // Arrange
            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

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
            var key = "test-flow";
            var routeValues = new RouteValueDictionary()
            {
                { "id", 42 },
                { "subid", 69 }
            };
            var instanceId = FormFlowInstanceId.GenerateForRouteValues(key, routeValues);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties: new Dictionary<object, object>()));

            var httpContext = new DefaultHttpContext();

            var routeData = new RouteData(new RouteValueDictionary());

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new FormFlowDescriptor(
                    key,
                    stateType,
                    IdGenerationSource.RouteValues,
                    idRouteParameterNames: new[] { "id", "subid" }));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

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
            var key = "test-flow";
            var stateType = typeof(TestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();

            var httpContext = new DefaultHttpContext();

            var routeData = new RouteData();

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

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
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns((FormFlowInstance)null);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        [Fact]
        public void TryResolveExistingInstance_MismatchingKeys_ReturnsNull()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            var state = new TestState();
            var descriptorKey = "another-key";

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties: new Dictionary<object, object>()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(descriptorKey, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

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
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            var state = new TestState();
            var descriptorStateType = typeof(OtherTestState);

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties: new Dictionary<object, object>()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, descriptorStateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

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
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties: new Dictionary<object, object>()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.True(result);
            Assert.NotNull(instance);
            Assert.Equal(key, instance.Key);
            Assert.Equal(instanceId, instance.InstanceId);
            Assert.Equal(stateType, instance.StateType);
        }

        [Fact]
        public void TryResolveExistingInstance_InstanceExistsForRouteValues_ReturnsInstance()
        {
            // Arrange
            var key = "test-flow";
            var routeValues = new RouteValueDictionary()
            {
                { "id", 42 },
                { "subid", 69 }
            };
            var instanceId = FormFlowInstanceId.GenerateForRouteValues(key, routeValues);
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(
                    FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties: new Dictionary<object, object>()));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/foo/42/69";

            var routeData = new RouteData(routeValues);

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(
                new FormFlowDescriptor(
                    key,
                    stateType,
                    IdGenerationSource.RouteValues,
                    idRouteParameterNames: new[] { "id", "subid" }));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.True(result);
            Assert.NotNull(instance);
            Assert.Equal(key, instance.Key);
            Assert.Equal(instanceId, instance.InstanceId);
            Assert.Equal(stateType, instance.StateType);
        }

        [Fact]
        public void TryResolveExistingInstance_InstanceIsDeleted_ReturnsFalse()
        {
            // Arrange
            var key = "test-flow";
            var instanceId = FormFlowInstanceId.GenerateForRandomId();
            var stateType = typeof(TestState);
            var state = new TestState();

            var stateProvider = new Mock<IUserInstanceStateProvider>();
            stateProvider
                .Setup(s => s.GetInstance(instanceId))
                .Returns(() =>
                {
                    var instance = FormFlowInstance.Create(
                        stateProvider.Object,
                        key,
                        instanceId,
                        stateType,
                        state,
                        properties: new Dictionary<object, object>());
                    instance.Deleted = true;
                    return instance;
                });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString($"?ffiid={instanceId}");

            var routeData = new RouteData(new RouteValueDictionary()
            {
                { "ffiid", instanceId }
            });

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.SetProperty(new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId));

            CreateActionContext(
                httpContext,
                routeData,
                actionDescriptor,
                out _,
                out var actionContextAccessor);

            var instanceProvider = new FormFlowInstanceProvider(stateProvider.Object, actionContextAccessor);

            // Act
            var result = instanceProvider.TryResolveExistingInstance(out var instance);

            // Assert
            Assert.False(result);
            Assert.Null(instance);
        }

        private static void CreateActionContext(
            HttpContext httpContext,
            RouteData routeData,
            ActionDescriptor actionDescriptor,
            out ActionContext actionContext,
            out IActionContextAccessor actionContextAccessor)
        {
            actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

            var actionContextAccessorMock = new Mock<IActionContextAccessor>();
            actionContextAccessorMock.SetupGet(mock => mock.ActionContext).Returns(actionContext);

            actionContextAccessor = actionContextAccessorMock.Object;
        }

        private class TestState { }

        private class OtherTestState { }
    }
}
