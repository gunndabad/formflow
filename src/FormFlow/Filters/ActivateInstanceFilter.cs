using System;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace FormFlow.Filters;

internal class ActivateInstanceFilter : IResourceFilter
{
    private readonly JourneyInstanceProvider _journeyInstanceProvider;

    public ActivateInstanceFilter(JourneyInstanceProvider journeyInstanceProvider)
    {
        ArgumentNullException.ThrowIfNull(journeyInstanceProvider);
        _journeyInstanceProvider = journeyInstanceProvider;
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var activatesJourneyMarker = context.ActionDescriptor.GetProperty<ActivatesJourneyMarker>();

        if (activatesJourneyMarker is null)
        {
            return;
        }

        if (_journeyInstanceProvider.TryResolveExistingInstance(context, out _))
        {
            return;
        }

        var journeyDescriptor = _journeyInstanceProvider.ResolveJourneyDescriptor(context, throwIfNotFound: true)!;
        var state = Activator.CreateInstance(journeyDescriptor.StateType)!;
        var newInstance = _journeyInstanceProvider.CreateInstance(context, state);

        if (journeyDescriptor.AppendUniqueKey)
        {
            // Need to redirect back to ourselves with the unique ID appended
            var currentUrl = context.HttpContext.Request.GetEncodedUrl();
            var newUrl = QueryHelpers.AddQueryString(currentUrl, Constants.UniqueKeyQueryParameterName, newInstance.InstanceId.UniqueKey!);
            context.Result = new RedirectResult(newUrl);
        }
    }
}
