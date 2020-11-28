using System.Collections.Generic;
using System.Net.Http;
using FormFlow.State;
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

        protected FormFlowInstance<TState> CreateInstanceForRouteParameters<TState>(
            string key,
            IReadOnlyDictionary<string, object> routeParameters,
            TState state,
            IReadOnlyDictionary<object, object> properties = null)
        {
            var instanceId = FormFlowInstanceId.GenerateForRouteValues(key, routeParameters);

            var instanceStateProvider = Fixture.Services.GetRequiredService<IUserInstanceStateProvider>();

            return (FormFlowInstance<TState>)instanceStateProvider.CreateInstance(
                key,
                instanceId,
                typeof(TState),
                state,
                properties);
        }
    }
}
