using Domain.Common.Models;
using Domain.Errors.Missions.RoutePlanning;
using FluentResults;

namespace Domain.Missions.ValueObjects;

public class RoutePlanningAlgorithm : ValueObject
{
    private enum Algorithm
    {
        DFS,
        GeneticAlgorithm,
        ReinforcementLearning
    }

    private Algorithm _value;

    private RoutePlanningAlgorithm(Algorithm value)
    {
        _value = value;
    }

    public static RoutePlanningAlgorithm Dfs => new(Algorithm.DFS);
    public static RoutePlanningAlgorithm GeneticAlgorithm => new(Algorithm.GeneticAlgorithm);
    public static RoutePlanningAlgorithm ReinforcementLearning => new(Algorithm.ReinforcementLearning);

    public static Result<RoutePlanningAlgorithm> FromString(string algorithm)
    {
        if (string.IsNullOrEmpty(algorithm))
        {
            return Result.Fail<RoutePlanningAlgorithm>(new InvalidAlgorithmError(algorithm));
        }

        return algorithm.ToLower() switch
        {
            "dfs" => Dfs,
            "geneticalgorithm" => GeneticAlgorithm,
            "reinforcementlearning" => ReinforcementLearning,
            _ => Result.Fail<RoutePlanningAlgorithm>(new InvalidAlgorithmError(algorithm))
        };
    }

    public override string ToString() => _value.ToString();
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
