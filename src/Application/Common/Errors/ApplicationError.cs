using FluentResults;

namespace Application.Common.Errors;

public sealed class ApplicationError : IError
{
    public List<IError> Reasons { get; }
    public string Message { get; }
    public Dictionary<string, object> Metadata { get; }

    public ApplicationError(string message, string code, string detail = "", List<IError>? domainErrors = null)
    {
        if (domainErrors == null)
        {
            Reasons = [];
        }
        else
        {
            Reasons = domainErrors;
        }
        Message = message;
        Metadata = new Dictionary<string, object> {
            { "code", code },
            { "detail", detail }
        };
    }

    public static ApplicationError NotFound(string message) => new(message, "NotFound");
    public static ApplicationError Duplicated(string message) => new(message, "Duplicated");
    public static ApplicationError Validation(string message) => new(message, "Validation");
    public static ApplicationError Forbidden(string message) => new(message, "Forbidden");    
    public static ApplicationError Internal => new("Infrastructure error", "Internal"); 
}