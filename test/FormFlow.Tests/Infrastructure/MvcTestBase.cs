using System.Collections.Generic;
using System.Net.Http;
using FormFlow.State;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FormFlow.Tests.Infrastructure
{
    [Collection("Mvc")]
    public abstract class MvcTestBase
    {
        protected MvcTestBase(MvcTestFixture fixture)
        {
            Fixture = fixture;

            StateProvider.Clear();
        }

        protected MvcTestFixture Fixture { get; }

        protected HttpClient HttpClient => Fixture.HttpClient;

        protected InMemoryInstanceStateProvider StateProvider =>
            (InMemoryInstanceStateProvider)Fixture.Services.GetRequiredService<IUserInstanceStateProvider>();

        protected JourneyInstance<TState> CreateInstance<TState>(
            string key,
            IReadOnlyDictionary<string, object> routeParameters,
            TState state,
            IReadOnlyDictionary<object, object> properties = null,
            string randomExtension = null)
        {
            var routeValues = new RouteValueDictionary(routeParameters);

            if (randomExtension != null)
            {
                routeValues.Add(Constants.RandomExtensionQueryParameterName, randomExtension);
            }

            var instanceId = new JourneyInstanceId(key, routeValues);

            var instanceStateProvider = Fixture.Services.GetRequiredService<IUserInstanceStateProvider>();

            return (JourneyInstance<TState>)instanceStateProvider.CreateInstance(
                key,
                instanceId,
                typeof(TState),
                state,
                properties);
        }
    }
}
