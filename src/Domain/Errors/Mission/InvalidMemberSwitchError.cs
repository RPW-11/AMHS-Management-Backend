namespace Domain.Errors.Mission;

public class InvalidMemberSwitchError : DomainError
{
    public InvalidMemberSwitchError(string fromRole, string toRole) 
    : base($"Cannot switch from {fromRole} to {toRole}", "Mission.InvalidMemberSwitchError")
    {
    }
}
