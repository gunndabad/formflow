using System;
using System.Net.Http;
using FormFlow.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FormFlow.Tests.Infrastructure
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
                            services
                                .AddMvc()
                                .AddNewtonsoftJson();

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

        public void Dispose()
        {
            HttpClient.Dispose();
            _host.Dispose();
        }
    }

    [CollectionDefinition("Mvc")]
    public class MvcTestCollection : ICollectionFixture<MvcTestFixture> { }
}
