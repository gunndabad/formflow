using System.Collections.Generic;
using System.Linq;
using FormFlow.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace FormFlow;

internal class FormFlowConventions :
    IPageApplicationModelConvention,
    IPageHandlerModelConvention,
    IControllerModelConvention,
    IActionModelConvention
{
    public void Apply(PageApplicationModel model) => ApplyConventions(model.HandlerTypeAttributes, model.Properties);

    public void Apply(PageHandlerModel model) => ApplyConventions(model.Attributes, model.Properties);

    public void Apply(ControllerModel controller) => ApplyConventions(controller.Attributes, controller.Properties);

    public void Apply(ActionModel action) => ApplyConventions(action.Attributes, action.Properties);

    internal static void ApplyConventions(IReadOnlyList<object> attributes, IDictionary<object, object?> properties)
    {
        var journeyAttribute = attributes.OfType<JourneyAttribute>().SingleOrDefault();

        if (journeyAttribute is not null)
        {
            properties.Add(typeof(ActionJourneyMetadata), new ActionJourneyMetadata(journeyAttribute.JourneyName));
        }

        var requireInstanceAttribute = attributes.OfType<RequireJourneyInstanceAttribute>().SingleOrDefault();

        if (requireInstanceAttribute is not null)
        {
            properties.Add(typeof(RequireInstanceMarker), new RequireInstanceMarker(requireInstanceAttribute.ErrorStatusCode));
        }
    }
}
