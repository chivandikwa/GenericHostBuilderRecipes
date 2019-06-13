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