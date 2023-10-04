using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace FormFlow;

internal sealed class ActionJourneyMetadata
{
    public ActionJourneyMetadata(string journeyName)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        JourneyName = journeyName;
    }

    public string JourneyName { get; }
}

internal static class ActionContextExtensions
{
    public static ActionJourneyMetadata? GetActionJourneyMetadata(this ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.ActionDescriptor.GetProperty<ActionJourneyMetadata>();
    }
}
