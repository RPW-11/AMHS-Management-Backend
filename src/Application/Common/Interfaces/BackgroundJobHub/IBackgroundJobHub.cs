namespace Application.Common.Interfaces.BackgroundJobHub;

public interface IBackgroundJobHub
{
    Task<Guid> EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> work);
}