namespace Application.DTOs.Mission;

public record UpdateMissionDto(
    string Name, 
    string Description, 
    string Status 
);
