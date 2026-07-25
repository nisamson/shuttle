namespace Shuttle.EFCore.Recruitment;

/// <summary>
/// Produces a <see cref="RecruitmentAnalysis"/> from the current database contents. Reusable by the
/// offline analysis CLI and by the API server.
/// </summary>
public interface IRecruitmentAnalysisService {

    /// <summary>
    /// Reads all players and member names and returns the aggregated recruitment analysis.
    /// </summary>
    Task<RecruitmentAnalysis> GetRecruitmentAnalysisAsync(CancellationToken cancellationToken = default);
}
