using System.Globalization;
using System.Text;
using Shuttle.EFCore.Recruitment;

namespace Shuttle.Analysis.Flows.Recruitment;

/// <summary>
/// Builds GraphViz DOT documents from a <see cref="RecruitmentAnalysis"/>: a full recruiter → member
/// graph (all categories, colored by recruiter type) and a focused member → member "player-recruiter
/// network" containing only recruiters that are themselves SHL members.
/// </summary>
public static class RecruitmentDotWriter {

    /// <summary>
    /// Builds the full recruiter → recruited-member digraph. Recruiter nodes are colored by category
    /// and recruited members are plain nodes; each edge is one recruited member.
    /// </summary>
    public static string BuildFullGraph(RecruitmentAnalysis analysis) {
        ArgumentNullException.ThrowIfNull(analysis);

        var sb = new StringBuilder();
        sb.AppendLine("digraph recruitment {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [style=filled, fillcolor=white, fontname=\"Helvetica\"];");

        foreach (var tally in analysis.Tallies) {
            var recruiterNode = RecruiterNodeId(tally.Category, tally.Recruiter);
            var label = tally.Category is RecruiterCategory.Self or RecruiterCategory.None
                ? CategoryLabel(tally.Category)
                : tally.Recruiter;
            sb.Append("  ").Append(recruiterNode)
                .Append(" [label=").Append(Quote(label))
                .Append(", fillcolor=").Append(Quote(CategoryColor(tally.Category)))
                .AppendLine("];");

            foreach (var edge in tally.Edges) {
                var memberNode = MemberNodeId(edge.UserId);
                sb.Append("  ").Append(memberNode)
                    .Append(" [label=").Append(Quote(edge.Username)).AppendLine("];");
                sb.Append("  ").Append(recruiterNode).Append(" -> ").Append(memberNode).AppendLine(";");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Builds the member → member player-recruiter network: only recruiters classified as SHL members
    /// (<see cref="RecruiterCategory.Player"/>) appear, with an edge from the recruiter member to each
    /// member they recruited, labeled with that member's career TPE.
    /// </summary>
    public static string BuildPlayerRecruiterNetwork(RecruitmentAnalysis analysis) {
        ArgumentNullException.ThrowIfNull(analysis);

        var sb = new StringBuilder();
        sb.AppendLine("digraph player_recruiter_network {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [style=filled, fillcolor=" + Quote(CategoryColor(RecruiterCategory.Player))
            + ", fontname=\"Helvetica\"];");

        foreach (var tally in analysis.Tallies.Where(t => t.Category == RecruiterCategory.Player)) {
            var recruiterNode = MemberNodeName(tally.Recruiter);
            sb.Append("  ").Append(recruiterNode)
                .Append(" [label=").Append(Quote(tally.Recruiter)).AppendLine("];");

            foreach (var edge in tally.Edges) {
                var memberNode = MemberNodeName(edge.Username);
                sb.Append("  ").Append(memberNode)
                    .Append(" [label=").Append(Quote(edge.Username)).AppendLine("];");
                sb.Append("  ").Append(recruiterNode).Append(" -> ").Append(memberNode)
                    .Append(" [label=").Append(Quote(edge.CareerTpe.ToString(CultureInfo.InvariantCulture)))
                    .AppendLine("];");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string CategoryColor(RecruiterCategory category) => category switch {
        RecruiterCategory.Player => "lightblue",
        RecruiterCategory.External => "orange",
        RecruiterCategory.Self => "lightgreen",
        RecruiterCategory.None => "lightgrey",
        _ => "white",
    };

    private static string CategoryLabel(RecruiterCategory category) => category switch {
        RecruiterCategory.Self => "(self)",
        RecruiterCategory.None => "(none)",
        _ => category.ToString(),
    };

    private static string RecruiterNodeId(RecruiterCategory category, string recruiter) =>
        MemberNodeName($"r_{category}_{recruiter}");

    private static string MemberNodeId(int userId) =>
        "u" + userId.ToString(CultureInfo.InvariantCulture);

    // A DOT node id derived from an arbitrary string: quote it and escape, so any characters are legal.
    private static string MemberNodeName(string value) => Quote(value);

    private static string Quote(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", string.Empty) + "\"";
}
