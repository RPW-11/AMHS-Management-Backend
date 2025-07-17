using Domain.Entities;
using Domain.Interfaces;

namespace Domain.Events;

public record EmployeeCheckedInEvent(Guid EmployeeId): IDomainEvent;
public record EmployeeCheckedOutEvent(Guid EmployeeId): IDomainEvent;
public record EmployeeReturnFromBreakEvent(Guid EmployeeId): IDomainEvent;
