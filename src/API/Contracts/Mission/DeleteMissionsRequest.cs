
namespace API.Contracts.Mission;

public record DeleteMissionsRequest(
    IEnumerable<string> MissionIds
);