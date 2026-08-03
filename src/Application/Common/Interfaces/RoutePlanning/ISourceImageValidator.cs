using FluentResults;

namespace Application.Common.Interfaces.RoutePlanning;

/// <summary>
/// Checks that an uploaded factory layout image can actually be decoded before a route planning
/// job is enqueued. Without this the first attempt to read the image happens inside the background
/// job, after the queue slot and the whole solve have already been spent.
/// </summary>
public interface ISourceImageValidator
{
    /// <param name="imageBytes">The uploaded file, already materialized.</param>
    /// <param name="contentType">The content type declared by the client, if any.</param>
    Result Validate(byte[] imageBytes, string? contentType);
}
