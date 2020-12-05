using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FormFlow
{
    public static class RedirectToActionResultExtensions
    {
        public static RedirectToActionResult WithJourneyInstance(
            this RedirectToActionResult result,
            JourneyInstance instance)
        {
            return WithJourneyInstance(result, instance.InstanceId);
        }

        public static RedirectToActionResult WithJourneyInstance(
            this RedirectToActionResult result,
            JourneyInstanceId instanceId)
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
