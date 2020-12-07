using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace FormFlow
{
    internal class FormFlowOptionsSetup : IConfigureOptions<FormFlowOptions>
    {
        public void Configure(FormFlowOptions options)
        {
            options.ValueProviderFactories.Add(new RouteValueProviderFactory());
            options.ValueProviderFactories.Add(new QueryStringValueProviderFactory());
        }
    }
}
