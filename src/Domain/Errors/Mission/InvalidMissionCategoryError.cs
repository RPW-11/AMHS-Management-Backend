namespace Domain.Errors.Mission;

public class InvalidMissionCategoryError : DomainError
{
    public InvalidMissionCategoryError(string category) 
    : base("Invalid mission category", "Mission.InvalidMissionCategory", $"The mission category '{category}' is invalid")
    {
    }
}
