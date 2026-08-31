using FluentResults;

namespace Application.Services.RoutePlanningService;

internal static class SolvedResult
{
    /// <summary>
    /// Unwraps a domain object rebuilt from an already-validated one. A failure here means the
    /// solve corrupted something rather than that the request was bad, so it throws: the job
    /// handler's catch is what turns it into a failed mission.
    /// </summary>
    public static T Require<T>(Result<T> result, string context)
    {
        if (result.IsFailed)
        {
            throw new InvalidOperationException($"{context}: {result.Errors[0].Message}");
        }

        return result.Value;
    }
}
