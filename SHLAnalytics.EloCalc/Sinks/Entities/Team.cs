using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndexTeam = SHLAnalytics.Api.Models.Index.V1.Team;

namespace SHLAnalytics.EloCalc.Sinks.Entities;

[EntityTypeConfiguration(typeof(TeamEntityConfiguration))]
public record Team {
    public Team() { }

    [SetsRequiredMembers]
    public Team(IndexTeam team) {
        TeamId = team.Id;
        Season = team.Season;
        Abbreviation = team.Abbreviation;
        PrimaryColor = team.Colors.Primary;
    }

    public int TeamId { get; set; }
    
    public int Season { get; set; }
    public required string Abbreviation { get; set; }
    public required Color PrimaryColor { get; set; }
}

public class TeamEntityConfiguration : IEntityTypeConfiguration<Team> {

    public void Configure(EntityTypeBuilder<Team> builder) {
        builder.HasKey(t => new { t.TeamId, t.Season });
        builder.Property(t => t.PrimaryColor)
            .HasConversion<string>(
                c => ColorTranslator.ToHtml(c),
                s => ColorTranslator.FromHtml(s)
            );
    }
}
