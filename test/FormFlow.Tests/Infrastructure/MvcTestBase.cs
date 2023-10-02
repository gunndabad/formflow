using System.Collections.Generic;
using System.Net.Http;
using FormFlow.State;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
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
            string journeyName,
            IReadOnlyDictionary<string, StringValues> keys,
            TState state,
            IReadOnlyDictionary<object, object>? properties = null,
            string? uniqueKey = null)
            where TState : notnull
        {
            var routeValues = new RouteValueDictionary(keys);

            if (uniqueKey != null)
            {
                routeValues.Add(Constants.UniqueKeyQueryParameterName, uniqueKey);
            }

            var instanceId = new JourneyInstanceId(journeyName, keys);

            var instanceStateProvider = Fixture.Services.GetRequiredService<IUserInstanceStateProvider>();

            return (JourneyInstance<TState>)instanceStateProvider.CreateInstance(
                journeyName,
                instanceId,
                typeof(TState),
                state,
                properties);
        }
    }
}
