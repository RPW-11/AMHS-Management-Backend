
using Application.DTOs.Mission.RoutePlanning;

namespace Application.DTOs.Mission;

public record MissionDto
(
    string Id,
    string Name, 
    string Description, 
    string Category, 
    string Status, 
    DateTime FinishedAt, 
    string? ResourceLink, 
    DateTime CreatedAt, 
    DateTime UpdatedAt,
    IEnumerable<AssignedEmployeeDto>? AssignedEmployees,
    RoutePlanningSummaryDto? RoutePlanningSummary 
);

public record AddMissionDto (string Id);

public record AssignedEmployeeDto(
    string Id,
    string Role
);
