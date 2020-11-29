using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
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

            var requireInstanceMarker = context.ActionDescriptor.GetProperty<RequireInstanceMarker>();

            if (requireInstanceMarker != null)
            {
                var flowDescriptor = FlowDescriptor.FromActionContext(context);
                if (flowDescriptor == null)
                {
                    throw new InvalidOperationException("No flow metadata found on action.");
                }

                var instanceProvider =
                    context.HttpContext.RequestServices.GetRequiredService<FormFlowInstanceProvider>();

                if (instanceProvider.GetInstance() == null)
                {
                    context.Result = requireInstanceMarker.ErrorStatusCode.HasValue ?
                        new StatusCodeResult(requireInstanceMarker.ErrorStatusCode.Value) :
                        options.MissingInstanceHandler(flowDescriptor, context.HttpContext);
                }
            }
        }
    }

    internal sealed class RequireInstanceMarker
    {
        public RequireInstanceMarker(int? errorStatusCode)
        {
            ErrorStatusCode = errorStatusCode;
        }

        public int? ErrorStatusCode { get; }
    }
}
