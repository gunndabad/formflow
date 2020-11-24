using System;
using System.Collections.Generic;
using System.Net.Http;
using FormFlow.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FormFlow.Tests
{
    public sealed class MvcTestFixture : IDisposable
    {
        private readonly IHost _host;

        public MvcTestFixture()
        {
            _host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices((ctx, services) =>
                        {
                            services.AddMvc();

                            services.AddFormFlow();

                            services.AddSingleton<IUserInstanceStateProvider, InMemoryInstanceStateProvider>();
                        })
                        .Configure(app =>
                        {
                            app.UseRouting();

                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapControllers();
                            });
                        });
                })
                .StartAsync().GetAwaiter().GetResult();

            Services = _host.Services;
            HttpClient = _host.GetTestClient();
        }

        public IServiceProvider Services { get; }

        public HttpClient HttpClient { get; }

        public void Dispose() => _host.Dispose();
    }

    [CollectionDefinition("Mvc")]
    public class MvcTestCollection : ICollectionFixture<MvcTestFixture> { }

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
