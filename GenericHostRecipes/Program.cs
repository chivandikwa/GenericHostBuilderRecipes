using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Sinks.Splunk;

using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace GenericHostBuilderRecipes
{
    class Program
    {
        static async Task Main()
        {
            var container = new Container();
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            IHost host = new HostBuilder()
                       .ConfigureHostConfiguration(builder =>
                       {
                           builder.AddJsonFile("hostsettings.json", optional: true);
                       })
                       .ConfigureAppConfiguration((hostContext, builder) =>
                       {
                           builder.AddJsonFile("appsettings.json");
                           builder.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                       })
                       .ConfigureLogging((hostContext, builder) =>
                       {
                           LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                                                     .ReadFrom.Configuration(hostContext.Configuration)
                                                     .WriteTo.Udp(IPAddress.Loopback, 514, new SplunkJsonFormatter(true, CultureInfo.InvariantCulture))
                                                     .Enrich.WithDemystifiedStackTraces()
                                                     .Enrich.WithProperty("version", "1.0.0.1");

                           Log.Logger = loggerConfiguration.CreateLogger();

                           builder.AddConsole();
                           builder.AddDebug();

                           builder.AddSerilog(dispose: true);
                       })
                       .ConfigureContainer<ServiceCollection>((builder, services) =>
                       {
                           // Using simple injector instead of ServiceCollection
                       })
                       .ConfigureServices((hostContext, services) =>
                       {
                           //If not using simple injector
                           // services.AddHostedService<Service>();
                           // services.AddOptions();
                           // Not using IOptions
                           var settings = hostContext.Configuration.GetSection("Configuration").Get<Settings>();
                           
                           services.AddSimpleInjector(container, options =>
                                                      {
                                                          container.RegisterInstance(settings);
                                                          container.Register<IScopedProcessingService, ScopedProcessingService>(Lifestyle.Scoped);
                                                          options.AddHostedService<LifetimeEventsHostedService>();
                                                          options.AddHostedService<HostedService>();
                                                      });
                           services.Configure<HostOptions>(option =>
                           {
                               option.ShutdownTimeout = TimeSpan.FromSeconds(20);
                           });
                       })
                       .UseConsoleLifetime()
                       .Build()
                       .UseSimpleInjector(container, options =>
                       {
                           options.UseLogging();
                       });

            container.Verify();

            using (host)
            {
                await host.StartAsync();
                await host.WaitForShutdownAsync();
            }
        }
    }
}
