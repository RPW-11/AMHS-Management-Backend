namespace Domain.Errors.Task;

public class InvalidTaskCategoryError : DomainError
{
    public InvalidTaskCategoryError(string category) 
    : base("Invalid task category", "Task.InvalidTaskCategory", $"The task category '{category}' is invalid")
    {
    }
}
