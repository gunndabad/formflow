using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FormFlow;

public class FormFlowOptions
{
    public FormFlowOptions()
    {
        ValueProviderFactories = new List<IValueProviderFactory>();
    }

    public MissingInstanceHandler MissingInstanceHandler { get; set; } = DefaultFormFlowOptions.MissingInstanceHandler;

    public IList<IValueProviderFactory> ValueProviderFactories { get; }
}
