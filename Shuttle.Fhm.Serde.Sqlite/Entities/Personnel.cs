using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Fhm.Serde.Domain.Files;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One editable personal.dat staff record.</summary>
public sealed class Personnel
{
    public int PersonnelId { get; set; }
    public int FirstNameNameId { get; set; }
    public Name FirstName { get; set; } = null!;
    public int SurnameNameId { get; set; }
    public Name Surname { get; set; } = null!;
    public int? NicknameNameId { get; set; }
    public Name? Nickname { get; set; }
    public DateOnly BirthDate { get; set; }
    public int NationalityId { get; set; }
    public int BirthCityId { get; set; }
    public int? TeamRecordOrdinal { get; set; }
    public Team? Team { get; set; }
    public FhmPersonnelJob Job { get; set; }
    public int? Negotiating { get; set; }
    public FhmOffensivePreference OffensivePreference { get; set; }
    public int? PlayerManagement { get; set; }
    public FhmPhysicalPreference PhysicalPreference { get; set; }
    public int? CoachingDefense { get; set; }
    public int? CoachingForwards { get; set; }
    public int? CoachingGoalies { get; set; }
    public int? CoachingProspects { get; set; }
    public int? EvaluateAbilities { get; set; }
    public int? EvaluatePotential { get; set; }
    public int Reputation { get; set; }
    public FhmLineMatchingTendency LineMatchingTendency { get; set; }
    public FhmGoalieHandlingTendency GoalieHandlingTendency { get; set; }
    public FhmVeteranPreference VeteranPreference { get; set; }
    public FhmInnovationTendency InnovationTendency { get; set; }
    public FhmLoyaltyTendency LoyaltyTendency { get; set; }
    public int Salary { get; set; }
    public int? ContractLength { get; set; }
    public bool Retired { get; set; }
    public int? DefensiveSkills { get; set; }
    public int? OffensiveSkills { get; set; }
    public int BasedInLocationId { get; set; }
    public int? PhysicalTraining { get; set; }
    public int? Tactics { get; set; }
    public int? Discipline { get; set; }
    public int? SelfPreservation { get; set; }
    public int? Motivation { get; set; }
    public int? IngameTactics { get; set; }
    public int? TrainerSkill { get; set; }
    public byte[] SerializedRecord { get; set; } = [];
}

public sealed class PersonnelConfiguration : IEntityTypeConfiguration<Personnel>
{
    private static readonly string[] RatingColumns =
    [
        nameof(Personnel.Negotiating),
        nameof(Personnel.PlayerManagement),
        nameof(Personnel.CoachingDefense),
        nameof(Personnel.CoachingForwards),
        nameof(Personnel.CoachingGoalies),
        nameof(Personnel.CoachingProspects),
        nameof(Personnel.EvaluateAbilities),
        nameof(Personnel.EvaluatePotential),
        nameof(Personnel.DefensiveSkills),
        nameof(Personnel.OffensiveSkills),
        nameof(Personnel.PhysicalTraining),
        nameof(Personnel.Tactics),
        nameof(Personnel.Discipline),
        nameof(Personnel.SelfPreservation),
        nameof(Personnel.Motivation),
        nameof(Personnel.IngameTactics),
        nameof(Personnel.TrainerSkill),
    ];

    public void Configure(EntityTypeBuilder<Personnel> builder)
    {
        builder.ToTable(
            "Personnel",
            table =>
            {
                foreach (var column in RatingColumns)
                {
                    table.HasCheckConstraint($"CK_Personnel_{column}", $"{column} BETWEEN 0 AND 20");
                }

                table.HasCheckConstraint(
                    "CK_Personnel_ContractLength",
                    "ContractLength IS NULL OR ContractLength BETWEEN 1 AND 255");
                table.HasCheckConstraint("CK_Personnel_NationalityId", "NationalityId BETWEEN 0 AND 65535");
                table.HasCheckConstraint("CK_Personnel_Reputation", "Reputation BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_Personnel_BasedInLocationId", "BasedInLocationId BETWEEN 0 AND 65535");
            });
        builder.HasKey(value => value.PersonnelId);
        builder.Property(value => value.PersonnelId).ValueGeneratedNever();
        builder.Property(value => value.SerializedRecord).IsRequired();
        builder.HasOne(value => value.FirstName)
            .WithMany()
            .HasForeignKey(value => value.FirstNameNameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.Surname)
            .WithMany()
            .HasForeignKey(value => value.SurnameNameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.Nickname)
            .WithMany()
            .HasForeignKey(value => value.NicknameNameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.Team)
            .WithMany(value => value.Staff)
            .HasForeignKey(value => value.TeamRecordOrdinal)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.FirstName).AutoInclude();
        builder.Navigation(value => value.Surname).AutoInclude();
        builder.Navigation(value => value.Nickname).AutoInclude();
    }
}
