using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microwave.Queries.Polling;

namespace Microwave.Queries.Handler
{

    public class BackgroundService<T> : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly PollingInterval<T> _pollingInterval;

        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        public BackgroundService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                _pollingInterval = scope.ServiceProvider.GetService<PollingInterval<T>>();
            }
        }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            _executingTask = ExecuteAsync(_stoppingCts.Token);

            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite,
                    cancellationToken));
            }
        }

        protected virtual async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                var now = DateTime.UtcNow;
                var nextTrigger = _pollingInterval.Next;
                var timeSpan = nextTrigger - now;
                await Task.Delay(timeSpan);
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetService<T>();

                    if (service is IAsyncEventHandler asynchandler) await asynchandler.Update();
                    if (service is IQueryEventHandler queryHandler) await queryHandler.Update();
                    if (service is IReadModelEventHandler readModelhandler) await readModelhandler.Update();
                }
            }
            while (!stoppingToken.IsCancellationRequested);
        }
    }
}