namespace Application.Common.Interfaces.BackgroundJobHub;

public interface IBackgroundJobHub
{
    bool TryEnqueue(Func<IServiceProvider, CancellationToken, Task> work, out Guid jobId);
}