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
        var errorDetail = error.Metadata["detail"].ToString();
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
