
using Domain.Missions.ValueObjects;

namespace Application.DTOs.RoutePlanning;

public record RouteSolutionDto(RgvMap RgvMap, RoutePlanningScoreDto Score);
