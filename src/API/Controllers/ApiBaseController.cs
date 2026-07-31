using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class ApiBaseController : ControllerBase
{
    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.ValueOrDefault);
        }

        return HandleError(result);
    }

    /// <summary>
    /// Maps a failed result to a response for endpoints that build their own success response,
    /// so an error reports the same status here as it would through <see cref="HandleResult{T}"/>.
    /// </summary>
    protected ActionResult HandleError(ResultBase result)
    {
        var firstError = result.Errors[0];
        return firstError switch
        {
            var e when e.HasMetadataKey("code") => HandleErrorWithMetadata(e),
            _ => Problem(statusCode: 500, title: "Internal Server Error")
        };
    }

    private ActionResult HandleErrorWithMetadata(IError error)
    {
        var errorType = error.Metadata["code"].ToString();

        var errorDetail = error.Metadata.TryGetValue("detail", out var detail) ? detail?.ToString() : null;
        return errorType switch
        {
            "Validation" => Problem(statusCode: 400, title: error.Message, detail: errorDetail),
            "NotFound" => Problem(statusCode: 404, title: error.Message, detail: errorDetail),
            "Duplicated" => Problem(statusCode: 409, title: error.Message, detail: errorDetail),
            "Forbidden" => Problem(statusCode: 403, title: error.Message, detail: errorDetail),
            _ => Problem(statusCode: 500, title: error.Message, detail: errorDetail)
        };
    }
}
