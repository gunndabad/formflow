using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FormFlow.Filters;

internal class MissingInstanceFilter : IAsyncResourceFilter
{
    private readonly IOptions<FormFlowOptions> _optionsAccessor;
    private readonly JourneyInstanceProvider _journeyInstanceProvider;

    public MissingInstanceFilter(IOptions<FormFlowOptions> optionsAccessor, JourneyInstanceProvider journeyInstanceProvider)
    {
        _optionsAccessor = optionsAccessor;
        _journeyInstanceProvider = journeyInstanceProvider;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var requireInstanceMarker = context.ActionDescriptor.GetProperty<RequireInstanceMarker>();

        if (requireInstanceMarker is null)
        {
            await next();
            return;
        }

        var instance = await _journeyInstanceProvider.ResolveCurrentInstanceAsync(context);
        if (instance is not null)
        {
            await next();
            return;
        }

        var journeyDescriptor = _journeyInstanceProvider.ResolveJourneyDescriptor(context, throwIfNotFound: true)!;
        context.Result = _optionsAccessor.Value.MissingInstanceHandler(journeyDescriptor, context.HttpContext, requireInstanceMarker.ErrorStatusCode);
    }
}
