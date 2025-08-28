namespace Domain.Errors.Mission;

public class NotFoundMemberError : DomainError
{
    public NotFoundMemberError() : base("The member does not exist in the mission", "Mission.NotFoundMember")
    {
    }
}
