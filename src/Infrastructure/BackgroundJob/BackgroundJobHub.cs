using System.Collections.Concurrent;
using System.Threading.Channels;
using Application.Common.Interfaces.BackgroundJobHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJob;


public record JobItem(
    Guid Id,
    Func<IServiceProvider, CancellationToken, Task> Work
);

public class BackgroundJobHub : BackgroundService, IBackgroundJobHub
{
    private readonly Channel<JobItem> _channel;
    private readonly ILogger<BackgroundJobHub> _logger;
    private readonly ConcurrentDictionary<Guid, string> _statuses;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _maxConcurrentWorkers;

    public BackgroundJobHub(
        ILogger<BackgroundJobHub> logger,
        IServiceScopeFactory scopeFactory,
        int maxConcurrentWorkers = 4,
        int maxQueueSize = 16)
    {
        _statuses = new();
        _logger = logger;
        _scopeFactory = scopeFactory;
        _maxConcurrentWorkers = maxConcurrentWorkers;

        var options = new BoundedChannelOptions(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<JobItem>(options);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = new List<Task>();
        for (int i = 0; i < _maxConcurrentWorkers; i++)
        {
            workers.Add(Task.Run(() => WorkerLoopAsync(stoppingToken), CancellationToken.None));
        }

        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background job workers stopped");
        }
    }

    private async Task WorkerLoopAsync(CancellationToken ct)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
        {
            using var scope = _scopeFactory.CreateScope();

            try
            {
                _statuses[job.Id] = "Processing...";
                await job.Work(scope.ServiceProvider, ct);
                _statuses[job.Id] = "Completed";
            }
            catch (OperationCanceledException)
            {
                _statuses[job.Id] = "Cancelled";
            }
            catch (Exception ex)
            {
                _statuses[job.Id] = $"Failed: {ex.Message}";
                _logger.LogError(ex, "Error processing job {JobId}", job.Id);
            }
        }
    }

    public bool TryEnqueue(Func<IServiceProvider, CancellationToken, Task> work, out Guid jobId)
    {
        jobId = Guid.NewGuid();

        if (!_channel.Writer.TryWrite(new JobItem(jobId, work)))
        {
            _logger.LogWarning("Job queue is full, rejected job {JobId}", jobId);
            return false;
        }

        _statuses[jobId] = "Queued";
        return true;
    }
}
