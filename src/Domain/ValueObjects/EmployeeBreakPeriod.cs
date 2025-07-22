namespace Domain.ValueObjects;

public record EmployeeBreakPeriod
{
    public TimeSpan StartTime { get; private set; }
    public TimeSpan? EndTime { get; private set; }

    public EmployeeBreakPeriod(TimeSpan startTime, TimeSpan? endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public void SetStartTime(TimeSpan startTime)
    {
        StartTime = startTime;
    }

    public void SetEndTime(TimeSpan endTime)
    {
        EndTime = endTime;
    }
}
