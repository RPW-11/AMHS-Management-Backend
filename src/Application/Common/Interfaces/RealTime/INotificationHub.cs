using System.Threading.Channels;
using Domain.Employees.ValueObjects;

namespace Application.Common.Interfaces.RealTime;

public interface INotificationHub
{
    void CreateChannel(EmployeeId employeeId);
    bool TryRemoveChannel(EmployeeId employeeId);
    Task PublishToUserAsync(EmployeeId employeeId, string message);
    IAsyncEnumerable<string> ReadFromChannelAsync(EmployeeId employeeId, CancellationToken cancellationToken);
}