using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FormFlow
{
    public static class RedirectToActionResultExtensions
    {
        public static RedirectToActionResult WithFormFlowInstance(
            this RedirectToActionResult result,
            FormFlowInstance instance)
        {
            return WithFormFlowInstance(result, instance.InstanceId);
        }

        public static RedirectToActionResult WithFormFlowInstance(
            this RedirectToActionResult result,
            FormFlowInstanceId instanceId)
        {
            result.RouteValues ??= new RouteValueDictionary();

            foreach (var kvp in instanceId.RouteValues)
            {
                result.RouteValues[kvp.Key] = kvp.Value;
            }

            return result;
        }
    }
}
