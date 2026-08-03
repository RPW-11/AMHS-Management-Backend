using FluentResults;

namespace Domain.Errors;

public class DomainError : IError
{
    public List<IError> Reasons { get; }
    public string Message { get; }
    public Dictionary<string, object> Metadata { get; }

    public DomainError(string message, string domainCode, string detail = "")
    {
        Reasons = [];
        Message = message;
        Metadata = new Dictionary<string, object> {
            { "domainCode", domainCode },
            { "detail", detail }
        };
    }
}
