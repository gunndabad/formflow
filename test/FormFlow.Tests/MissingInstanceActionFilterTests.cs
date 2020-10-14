using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormFlow.Filters;
using FormFlow.Metadata;
using FormFlow.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace FormFlow.Tests
{
    public class MissingInstanceActionFilterTests
    {
        [Fact]
        public void OnActionExecuting_ActionHasNoFlowDescriptor_DoesNotSetResult()
        {
            // Arrange
            var key = "key";
            var stateType = typeof(MyState);

            MissingInstanceHandler handler = (flowDescriptor, httpContext) => new CustomResult();

            var options = new FormFlowOptions()
            {
                MissingInstanceHandler = handler
            };

            var services = new ServiceCollection()
                .AddSingleton(Options.Create(options))
                .AddSingleton<IUserInstanceStateProvider, InMemoryInstanceStateProvider>()
                .BuildServiceProvider();

            var flowDescriptor = new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services;

            var routeData = new RouteData();

            var actionDescriptor = new ActionDescriptor();

            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

            var actionArguments = new Dictionary<string, object>();

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                controller: null);

            var filter = new MissingInstanceActionFilter();

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_OptionsHaveNullHandler_DoesNotSetResult()
        {
            // Arrange
            var key = "key";
            var stateType = typeof(MyState);

            MissingInstanceHandler handler = null;

            var options = new FormFlowOptions()
            {
                MissingInstanceHandler = handler
            };

            var services = new ServiceCollection()
                .AddSingleton(Options.Create(options))
                .AddSingleton<IUserInstanceStateProvider, InMemoryInstanceStateProvider>()
                .BuildServiceProvider();

            var flowDescriptor = new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services;

            var routeData = new RouteData();

            var actionDescriptor = new ActionDescriptor()
            {
                Parameters = new List<ParameterDescriptor>()
                {
                    new ParameterDescriptor()
                    {
                        Name = "instance",
                        ParameterType = typeof(FormFlowInstance)
                    }
                }
            };
            actionDescriptor.SetProperty(flowDescriptor);

            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

            var actionArguments = new Dictionary<string, object>();

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                controller: null);

            var filter = new MissingInstanceActionFilter();

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_InstanceArgumentIsBound_DoesNotSetResult()
        {
            // Arrange
            var key = "key";
            var stateType = typeof(MyState);

            MissingInstanceHandler handler = (flowDescriptor, httpContext) => new CustomResult();

            var options = new FormFlowOptions()
            {
                MissingInstanceHandler = handler
            };

            var services = new ServiceCollection()
                .AddSingleton(Options.Create(options))
                .AddSingleton<IUserInstanceStateProvider, InMemoryInstanceStateProvider>()
                .BuildServiceProvider();

            var flowDescriptor = new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services;

            var routeData = new RouteData();

            var actionDescriptor = new ActionDescriptor()
            {
                Parameters = new List<ParameterDescriptor>()
                {
                    new ParameterDescriptor()
                    {
                        Name = "instance",
                        ParameterType = typeof(FormFlowInstance)
                    }
                }
            };
            actionDescriptor.SetProperty(flowDescriptor);

            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

            var actionArguments = new Dictionary<string, object>()
            {
                {
                    "instance",
                    FormFlowInstance.Create(
                        new InMemoryInstanceStateProvider(),
                        key,
                        FormFlowInstanceId.GenerateForRandomId(),
                        stateType,
                        new MyState(),
                        new Dictionary<object, object>())
                }
            };

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                controller: null);

            var filter = new MissingInstanceActionFilter();

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_InstanceArgumentIsNotBound_SetsResult()
        {
            // Arrange
            var key = "key";
            var stateType = typeof(MyState);

            MissingInstanceHandler handler = (flowDescriptor, httpContext) => new CustomResult();

            var options = new FormFlowOptions()
            {
                MissingInstanceHandler = handler
            };

            var services = new ServiceCollection()
                .AddSingleton(Options.Create(options))
                .AddSingleton<IUserInstanceStateProvider, InMemoryInstanceStateProvider>()
                .BuildServiceProvider();

            var flowDescriptor = new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services;

            var routeData = new RouteData();

            var actionDescriptor = new ActionDescriptor()
            {
                Parameters = new List<ParameterDescriptor>()
                {
                    new ParameterDescriptor()
                    {
                        Name = "instance",
                        ParameterType = typeof(FormFlowInstance)
                    }
                }
            };
            actionDescriptor.SetProperty(flowDescriptor);
            actionDescriptor.SetProperty(RequiresInstanceMarker.Instance);

            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

            var actionArguments = new Dictionary<string, object>();

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                controller: null);

            var filter = new MissingInstanceActionFilter();

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.IsType<CustomResult>(context.Result);
        }

        [Fact]
        public void OnActionExecuting_ActionDecoratedWithRequiresFormFlowInstanceAttributeAndNoInstance_SetsResult()
        {
            // Arrange
            var key = "key";
            var stateType = typeof(MyState);

            MissingInstanceHandler handler = (flowDescriptor, httpContext) => new CustomResult();

            var options = new FormFlowOptions()
            {
                MissingInstanceHandler = handler
            };

            var services = new ServiceCollection()
                .AddSingleton(Options.Create(options))
                .AddSingleton<IUserInstanceStateProvider, InMemoryInstanceStateProvider>()
                .BuildServiceProvider();

            var flowDescriptor = new FormFlowDescriptor(key, stateType, IdGenerationSource.RandomId);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services;

            var routeData = new RouteData();

            var actionDescriptor = new ActionDescriptor()
            {
                Parameters = new List<ParameterDescriptor>()
            };
            actionDescriptor.SetProperty(flowDescriptor);
            actionDescriptor.SetProperty(RequiresInstanceMarker.Instance);

            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

            var actionArguments = new Dictionary<string, object>();

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                controller: null);

            var filter = new MissingInstanceActionFilter();

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.IsType<CustomResult>(context.Result);
        }

        private class CustomResult : IActionResult
        {
            public Task ExecuteResultAsync(ActionContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class MyState { }
    }
}
