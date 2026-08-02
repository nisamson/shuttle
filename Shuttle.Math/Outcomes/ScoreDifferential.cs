namespace Shuttle.Math.Outcomes;

public record struct ScoreDifferential(int Differential)
{
    public static implicit operator ScoreDifferential(int differential) => new ScoreDifferential(differential);
    public static implicit operator int(ScoreDifferential scoreDifferential) => scoreDifferential.Differential;
}
