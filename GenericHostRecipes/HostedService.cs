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