namespace Domain.Errors.Missions.RoutePlanning;

public class InvalidStationName : DomainError
{
    public InvalidStationName(string stationName)
    : base("Invalid station with respect to the map matrix", "RgvMap.InvalidStationName", $"No station named '{stationName}' was found at its position in the map matrix")
    {
    }
}
