using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Application.Common.Interfaces.RealTime;
using Domain.Employees.ValueObjects;

namespace Infrastructure.RealTime;

public class NotificationHub : INotificationHub, IDisposable
{
    private readonly ConcurrentDictionary<EmployeeId, Channel<string>> _employeeChannels = new();

    public void CreateChannel(EmployeeId employeeId)
    {
        _employeeChannels.GetOrAdd(employeeId, _ => Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = false, // multiple SSE connections per user possible (multi-tab)
            SingleWriter = true
        }));
    }

    public bool TryRemoveChannel(EmployeeId employeeId)
    {
        return _employeeChannels.TryRemove(employeeId, out _);
    }

    public async Task PublishToUserAsync(EmployeeId employeeId, string message)
    {
        if (_employeeChannels.TryGetValue(employeeId, out var channel))
        {
            await channel.Writer.WriteAsync(message);
        }

    }
    public async IAsyncEnumerable<string> ReadFromChannelAsync(
        EmployeeId employeeId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_employeeChannels.TryGetValue(employeeId, out var channel))
        {
            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return message;
            }
        }
        yield return "";
    }
    public void Dispose()
    {
        foreach (var channel in _employeeChannels.Values)
        {
            channel.Writer.Complete();
        }
        _employeeChannels.Clear();
    }
}