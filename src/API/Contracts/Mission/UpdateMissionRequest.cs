namespace API.Contracts.Mission;

public record class UpdateMissionRequest
(
    string Name,
    string Description,
    string Status
);
