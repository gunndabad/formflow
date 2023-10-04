using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FormFlow.Filters;

public class MissingInstanceActionFilter : IActionFilter, IPageFilter
{
    private readonly IOptions<FormFlowOptions> _optionsAccessor;
    private readonly JourneyInstanceProvider _journeyInstanceProvider;

    public MissingInstanceActionFilter(IOptions<FormFlowOptions> optionsAccessor, JourneyInstanceProvider journeyInstanceProvider)
    {
        _optionsAccessor = optionsAccessor;
        _journeyInstanceProvider = journeyInstanceProvider;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context) => CheckInstance(context, result => context.Result = result);

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context) => CheckInstance(context, result => context.Result = result);

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    private void CheckInstance(ActionContext actionContext, Action<IActionResult> assignResult)
    {
        var requireInstanceMarker = actionContext.ActionDescriptor.GetProperty<RequireInstanceMarker>();

        if (requireInstanceMarker is not null)
        {
            if (!_journeyInstanceProvider.TryResolveExistingInstance(actionContext, out _))
            {
                var journeyDescriptor = _journeyInstanceProvider.ResolveJourneyDescriptor(actionContext, throwIfNotFound: true)!;

                assignResult(_optionsAccessor.Value.MissingInstanceHandler(journeyDescriptor, actionContext.HttpContext, requireInstanceMarker.ErrorStatusCode));
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
