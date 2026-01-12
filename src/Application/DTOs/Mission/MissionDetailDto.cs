using Application.DTOs.Mission.RoutePlanning;

namespace Application.DTOs.Mission;

public class MissionDetailDto
{
    public MissionDetailDto(string id,
                            string name,
                            string description,
                            string category,
                            string status,
                            AssignedEmployeeDto leader,
                            DateTime finishedAt,
                            string? resourceLink,
                            DateTime createdAt,
                            DateTime updatedAt,
                            int numberOfMembers,
                            RoutePlanningSummaryDto? routePlanningSummary)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
        Status = status;
        Leader = leader;
        FinishedAt = finishedAt;
        ResourceLink = resourceLink;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        NumberOfMembers = numberOfMembers;
        RoutePlanningSummary = routePlanningSummary;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Status { get; set; }
    public AssignedEmployeeDto Leader { get; set; }
    public DateTime FinishedAt { get; set; }
    public string? ResourceLink { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int NumberOfMembers { get; set; }
    public RoutePlanningSummaryDto? RoutePlanningSummary { get; set; }
}
