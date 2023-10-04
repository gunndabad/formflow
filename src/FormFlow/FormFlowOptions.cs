using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FormFlow;

public class FormFlowOptions
{
    private MissingInstanceHandler _missingInstanceHandler = DefaultFormFlowOptions.MissingInstanceHandler;

    public FormFlowOptions()
    {
        ValueProviderFactories = new List<IValueProviderFactory>()
        {
            new RouteValueProviderFactory(),
            new QueryStringValueProviderFactory()
        };

        JourneyRegistry = new();
    }

    public JourneyRegistry JourneyRegistry { get; }

    public MissingInstanceHandler MissingInstanceHandler
    {
        get => _missingInstanceHandler;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _missingInstanceHandler = value;
        }
    }

    public IList<IValueProviderFactory> ValueProviderFactories { get; }
}
