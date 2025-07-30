namespace Application.DTOs.Mission;

public record MissionDto
(
    Guid Id,
    string Name, 
    string Description, 
    string Category, 
    string Status, 
    DateTime? FinishedAt, 
    string? ResourceLink, 
    DateTime CreatedAt, 
    DateTime UpdatedAt 
);
