using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FormFlow.Filters;

internal class MissingInstanceFilter : IResourceFilter
{
    private readonly IOptions<FormFlowOptions> _optionsAccessor;
    private readonly JourneyInstanceProvider _journeyInstanceProvider;

    public MissingInstanceFilter(IOptions<FormFlowOptions> optionsAccessor, JourneyInstanceProvider journeyInstanceProvider)
    {
        _optionsAccessor = optionsAccessor;
        _journeyInstanceProvider = journeyInstanceProvider;
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var requireInstanceMarker = context.ActionDescriptor.GetProperty<RequireInstanceMarker>();

        if (requireInstanceMarker is null)
        {
            return;
        }

        if (_journeyInstanceProvider.TryResolveExistingInstance(context, out _))
        {
            return;
        }

        var journeyDescriptor = _journeyInstanceProvider.ResolveJourneyDescriptor(context, throwIfNotFound: true)!;
        context.Result = _optionsAccessor.Value.MissingInstanceHandler(journeyDescriptor, context.HttpContext, requireInstanceMarker.ErrorStatusCode);
    }
}
