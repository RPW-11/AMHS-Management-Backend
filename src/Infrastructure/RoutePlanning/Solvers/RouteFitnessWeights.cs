using Application.Common.Interfaces.RoutePlanning;

namespace Infrastructure.RoutePlanning.Solvers;

public sealed record RouteFitnessWeights(double ThroughputWeight, double LengthWeight, double NumOfRgvsWeight, double AlignmentRewardRate)
{
    private static readonly RouteFitnessWeights Connector = new(ThroughputWeight: 0.8, LengthWeight: 0.1, NumOfRgvsWeight: 0.1, AlignmentRewardRate: 100);
    private static readonly RouteFitnessWeights ClusterLoop = new(ThroughputWeight: 0.1, LengthWeight: 0.8, NumOfRgvsWeight: 0.1, AlignmentRewardRate: 0);

    public static RouteFitnessWeights For(RouteSolvePurpose purpose) => purpose switch
    {
        RouteSolvePurpose.ClusterLoop => ClusterLoop,
        RouteSolvePurpose.Connector => Connector,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unsupported route solve purpose")
    };
}
