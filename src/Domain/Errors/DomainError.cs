using FluentResults;

namespace Domain.Errors;

public class DomainError : IError
{
    public List<IError> Reasons { get; }
    public string Message { get; }
    public Dictionary<string, object> Metadata { get; }

    public DomainError(string message, string statusCode, string detail = "")
    {
        Reasons = [];
        Message = message;
        Metadata = new Dictionary<string, object> {
            { "statusCode", statusCode },
            { "detail", detail }
        };
    }
}
