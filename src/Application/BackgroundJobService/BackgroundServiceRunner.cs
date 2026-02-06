using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.BackgroundJobService;


public record JobItem(
    Guid Id,
    Func<IServiceProvider, CancellationToken, Task> Work
);

public class BackgroundServiceRunner : BackgroundService
{
    private readonly Channel<JobItem> _channel;
    private readonly ILogger<BackgroundServiceRunner> _logger;
    private readonly ConcurrentDictionary<Guid, string> _statuses;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _maxConcurrentWorkers;
    private readonly int _maxQueueSize;

    public BackgroundServiceRunner(
        ILogger<BackgroundServiceRunner> logger,
        IServiceScopeFactory scopeFactory,
        int maxConcurrentWorkers = 4,
        int maxQueueSize = 200)
    {
        _statuses = new();
        _logger = logger;
        _scopeFactory = scopeFactory;
        _maxConcurrentWorkers = maxConcurrentWorkers;
        _maxQueueSize = maxQueueSize;

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
            workers.Add(Task.Factory.StartNew(
                () => WorkerLoopAsync(stoppingToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default));
        }

        await Task.WhenAll(workers);
    }

    private async Task WorkerLoopAsync(CancellationToken ct)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
        {
            try
            {
                _statuses[job.Id] = "Processing...";
                await job.Work(_scopeFactory.CreateScope().ServiceProvider, ct);             
                _statuses[job.Id] = "Completed";
            }
            catch (OperationCanceledException)
            {
                _statuses[job.Id] = "Cancelled";
            }
            catch (Exception ex)
            {
                _statuses[job.Id] = $"Failed: {ex.Message}";
                _logger.LogError("Error processing the job: {Error}", ex);
            }
        }
    }

    public async Task<Guid> EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> work)
    {
        var id = Guid.NewGuid();
        _statuses[id] = "Queued";

        var item = new JobItem(id, work);

        await _channel.Writer.WriteAsync(item);
        return id;
    }
}
