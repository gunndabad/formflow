using System.Linq;
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
            var flowDescriptor = FormFlowDescriptor.FromActionContext(context);
            if (flowDescriptor == null)
            {
                return;
            }

            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<FormFlowOptions>>().Value;
            if (options.MissingInstanceHandler == null)
            {
                return;
            }

            var instanceParameters = context.ActionDescriptor.Parameters
                .Where(p => FormFlowInstance.IsFormFlowInstanceType(p.ParameterType))
                .ToArray();

            foreach (var p in instanceParameters)
            {
                if (!context.ActionArguments.TryGetValue(p.Name, out var instanceArgument) ||
                    instanceArgument == null)
                {
                    context.Result = options.MissingInstanceHandler(flowDescriptor, context.HttpContext);
                    return;
                }
            }

            if (context.ActionDescriptor.Properties.ContainsKey(typeof(RequiresInstanceMarker)))
            {
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
