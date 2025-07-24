namespace Domain.Errors.Task;

public class InvalidTaskStatusError : DomainError
{
    public InvalidTaskStatusError(string status) 
    : base("Invalid task status", "Task.InvalidTaskStatus", $"the task status '{status}' is invalid")
    {
    }
}
