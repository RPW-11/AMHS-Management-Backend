namespace Application.DTOs.Mission;

public record MissionDto
(
    string Id,
    string Name,
    string Description,
    string Category,
    string Status,
    DateTime FinishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);


