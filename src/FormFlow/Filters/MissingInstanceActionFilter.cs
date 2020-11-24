﻿using System;
using FormFlow.Metadata;
using FormFlow.State;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FormFlow.Filters
{
    public class MissingInstanceActionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<FormFlowOptions>>().Value;

            if (context.ActionDescriptor.Properties.ContainsKey(typeof(RequiresInstanceMarker)))
            {
                var flowDescriptor = FormFlowDescriptor.FromActionContext(context);
                if (flowDescriptor == null)
                {
                    throw new InvalidOperationException("No FormFlow metadata found on action.");
                }

                var instanceResolver = new InstanceResolver(
                    context.HttpContext.RequestServices.GetRequiredService<IUserInstanceStateProvider>());

                if (instanceResolver.Resolve(context) == null)
                {
                    context.Result = options.MissingInstanceHandler(flowDescriptor, context.HttpContext);
                    return;
                }
            }
        }
    }

    internal sealed class RequiresInstanceMarker
    {
        private RequiresInstanceMarker() { }

        public static RequiresInstanceMarker Instance { get; } = new RequiresInstanceMarker();
    }
}
