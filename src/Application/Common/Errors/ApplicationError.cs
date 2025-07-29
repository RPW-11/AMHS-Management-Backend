using System.Net;
using FluentResults;

namespace Application.Common.Errors;

public sealed class ApplicationError : IError
{
    public List<IError> Reasons { get; }
    public string Message { get; }
    public Dictionary<string, object> Metadata { get; }

    public ApplicationError(string message, string statusCode, string detail = "", List<IError>? domainErrors = null)
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
            { "statusCode", statusCode },
            { "detail", detail }
        };
    }
}