using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Fhm.Serde.Domain.Files;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

public sealed class PlayerContract
{
    public int PlayerInternalId { get; set; }
    public int ContractOrdinal { get; set; }

    public Player Player { get; set; } = null!;
    public ICollection<PlayerContractYear> Years { get; } = [];
}

public sealed class PlayerContractYear
{
    public int PlayerInternalId { get; set; }
    public int ContractOrdinal { get; set; }
    public int YearNumber { get; set; }
    public int? MajorLeagueSalary { get; set; }
    public int? MinorLeagueSalary { get; set; }

    public PlayerContract Contract { get; set; } = null!;
}

internal sealed class PlayerContractConfiguration : IEntityTypeConfiguration<PlayerContract>
{
    public void Configure(EntityTypeBuilder<PlayerContract> builder)
    {
        builder.ToTable("PlayerContracts");
        builder.HasKey(contract => new { contract.PlayerInternalId, contract.ContractOrdinal });
        builder.HasOne(contract => contract.Player)
            .WithMany(player => player.Contracts)
            .HasForeignKey(contract => contract.PlayerInternalId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(contract => contract.Years).AutoInclude();
    }
}

internal sealed class PlayerContractYearConfiguration : IEntityTypeConfiguration<PlayerContractYear>
{
    public void Configure(EntityTypeBuilder<PlayerContractYear> builder)
    {
        builder.ToTable(
            "PlayerContractYears",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_PlayerContractYears_YearNumber",
                    $"YearNumber BETWEEN 1 AND {FhmPlayerContract.MaximumYears}");
                table.HasCheckConstraint(
                    "CK_PlayerContractYears_MajorLeagueSalary",
                    "MajorLeagueSalary IS NULL OR MajorLeagueSalary >= 0");
                table.HasCheckConstraint(
                    "CK_PlayerContractYears_MinorLeagueSalary",
                    "MinorLeagueSalary IS NULL OR MinorLeagueSalary >= 0");
            });
        builder.HasKey(year => new { year.PlayerInternalId, year.ContractOrdinal, year.YearNumber });
        builder.HasOne(year => year.Contract)
            .WithMany(contract => contract.Years)
            .HasForeignKey(year => new { year.PlayerInternalId, year.ContractOrdinal })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
