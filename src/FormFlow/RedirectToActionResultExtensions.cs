using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FormFlow;

public static class RedirectToActionResultExtensions
{
    public static RedirectToActionResult WithJourneyInstanceUniqueKey(
        this RedirectToActionResult result,
        JourneyInstance instance)
    {
        return WithJourneyInstanceUniqueKey(result, instance.InstanceId);
    }

    public static RedirectToActionResult WithJourneyInstanceUniqueKey(
        this RedirectToActionResult result,
        JourneyInstanceId instanceId)
    {
        if (instanceId.UniqueKey == null)
        {
            throw new ArgumentException(
                "Specified instance does not have a unique key.",
                nameof(instanceId));
        }

        result.RouteValues ??= new RouteValueDictionary();
        result.RouteValues["ffiid"] = instanceId.UniqueKey;

        return result;
    }
}
