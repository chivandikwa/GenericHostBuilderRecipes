### Generic Host

The <code>WebHostBuilder</code> goodness that allow app configuration and launching for asp.net core apps has been brought to other application types in the form of the Generic Host Builder. This can be used for instance in a console app, to create a hosted service, simply a service that implement <code>IHostedService </code>.

```CSharp
  public interface IHostedService
  {
    // Triggered when the application host is ready to start the service.
    Task StartAsync(CancellationToken cancellationToken);

    // Triggered when the application host is performing a graceful shutdown.
    Task StopAsync(CancellationToken cancellationToken);
  }
```

### Why should I care?

Well, the goodness of the <code>WebHostBuilder</code> in configuration, dependency injection setup and logging can now be used in any hosting scenario like background tasks.

> While the Generic Host is not suitable for web hosting it has been [stated](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host) that when it is mature it will replace the Web Host as the single primary host API for all scenarios.

### How does this all work

The Generic Host library is available in the Microsoft.Extensions.Hosting package. Let's have a look at a complete sample and then break it down.

> I am a big fan of SimpleInjector and you will notice it is included in this example. For this to work make sure to install the nuget package that adds support of the Generic host to SimpleInjector first. <code>Install-Package SimpleInjector.Integration.GenericHost</code>

```csharp
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
                           builder.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                            optional: true);
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
                                                          container.Register<IScopedProcessingService,
                                                           ScopedProcessingService>(Lifestyle.Scoped);
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
```

The first thing is to create a new host builder. This gives access to fluent interface with a bunch of interesting methods.
<b>ConfigureHostConfiguration</b> Allows configuring of the builder itself and will be used to initialize the hosting environment. This can be called multiple times in an additive manner.
<b>ConfigureAppConfiguration</b> Allows configuration of the app itself. This can be called multiple times in an additive manner.
<b>ConfigureLogging</b> Allows configuration of logging in the application.
<b>ConfigureContainer</b> Allows for setting up the dependency injection. In our example I left this empty as I am not using the ServiceCollection but simple injector.
<b>ConfigureServices</b> Allows for registration of any services required in the dependency injection container. Here is where we registered simple injector.

When all this is done you call build on the host builder and can then startup the host. On startup the host will call <code>StartAsync</code> on any registered hosted services in order or registration and call <code>StopAsync</code> on stop. That is about all there is, the rest is in the hosted service implementation. Below I have two samples <code>HostedService</code> and <code>LifetimeEventsHostedService</code> below. The former has no hooks except being called on start and stop while the latter hooks onto life cycle events.

```csharp
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace GenericHostBuilderRecipes
{
    public class HostedService : IHostedService
    {
        private readonly Container _container;

        public HostedService(Container container)
        {
            _container = container;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            DoWork();

            return Task.CompletedTask;
        }

        private void DoWork()
        {
            using (AsyncScopedLifestyle.BeginScope(_container))
            {
                var service = _container.GetInstance<IScopedProcessingService>();
                service.DoWork();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace GenericHostBuilderRecipes
{
    public class LifetimeEventsHostedService : IHostedService
    {
        private readonly IApplicationLifetime _applicationLifetime;

        public LifetimeEventsHostedService(IApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _applicationLifetime.ApplicationStarted.Register(OnStarted);
            _applicationLifetime.ApplicationStopping.Register(OnStopping);
            _applicationLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
        }

        private void OnStopping()
        {
        }

        private void OnStopped()
        {
        }
    }
}
```

Checkout the complete code sample used in this post [on github](https://github.com/chivandikwa/GenericHostBuilderRecipes).
