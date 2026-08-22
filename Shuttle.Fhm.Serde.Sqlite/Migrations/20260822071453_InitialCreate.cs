using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Fhm.Serde.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameSettings",
                columns: table => new
                {
                    SettingOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    SettingType = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    ByteValue = table.Column<byte>(type: "INTEGER", nullable: true),
                    DoubleValue = table.Column<double>(type: "REAL", nullable: true),
                    Int32Value = table.Column<int>(type: "INTEGER", nullable: true),
                    StringValue = table.Column<string>(type: "TEXT", nullable: true),
                    UInt16Value = table.Column<ushort>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSettings", x => x.SettingOrdinal);
                });

            migrationBuilder.CreateTable(
                name: "ModifierCatalogues",
                columns: table => new
                {
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModifierCatalogues", x => new { x.RelativePath, x.RecordOrdinal });
                });

            migrationBuilder.CreateTable(
                name: "Names",
                columns: table => new
                {
                    NameId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryWeight = table.Column<int>(type: "INTEGER", nullable: false),
                    FlagA = table.Column<int>(type: "INTEGER", nullable: false),
                    FlagB = table.Column<int>(type: "INTEGER", nullable: false),
                    FlagC = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Names", x => x.NameId);
                });

            migrationBuilder.CreateTable(
                name: "NameScalars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameScalars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaveFiles",
                columns: table => new
                {
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveFiles", x => x.RelativePath);
                });

            migrationBuilder.CreateTable(
                name: "SaveManifest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceFormatVersion = table.Column<string>(type: "TEXT", nullable: false),
                    SourceFileCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveManifest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetPlays",
                columns: table => new
                {
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    IsExtraRecord = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetPlays", x => new { x.RelativePath, x.IsExtraRecord, x.RecordOrdinal });
                });

            migrationBuilder.CreateTable(
                name: "TacticalRoleCatalogue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionTag = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacticalRoleCatalogue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TacticalRoles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    AppliesToForwards = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliesToDefencemen = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliesToGoalies = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleFlags = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    TuningValueA = table.Column<int>(type: "INTEGER", nullable: false),
                    TuningValueB = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    TuningValueC = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacticalRoles", x => x.RoleId);
                    table.CheckConstraint("CK_PlayerRoles_AppliesToDefencemen", "AppliesToDefencemen BETWEEN 0 AND 255");
                    table.CheckConstraint("CK_PlayerRoles_AppliesToForwards", "AppliesToForwards BETWEEN 0 AND 255");
                    table.CheckConstraint("CK_PlayerRoles_AppliesToGoalies", "AppliesToGoalies BETWEEN 0 AND 255");
                    table.CheckConstraint("CK_PlayerRoles_PositionCategory", "PositionCategory BETWEEN 0 AND 65535");
                    table.CheckConstraint("CK_PlayerRoles_RoleFlags", "RoleFlags BETWEEN 0 AND 255");
                    table.CheckConstraint("CK_PlayerRoles_TuningValueA", "TuningValueA BETWEEN 0 AND 65535");
                    table.CheckConstraint("CK_PlayerRoles_TuningValueB", "TuningValueB BETWEEN 0 AND 65535");
                    table.CheckConstraint("CK_PlayerRoles_TuningValueC", "TuningValueC BETWEEN 0 AND 65535");
                });

            migrationBuilder.CreateTable(
                name: "TacticFiles",
                columns: table => new
                {
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacticFiles", x => x.RelativePath);
                });

            migrationBuilder.CreateTable(
                name: "Tactics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    TacticCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordsOpaque = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tactics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TacticSystems",
                columns: table => new
                {
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    GlobalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneGroupRaw = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    RatingA = table.Column<int>(type: "INTEGER", nullable: false),
                    RatingB = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacticSystems", x => x.RecordOrdinal);
                });

            migrationBuilder.CreateTable(
                name: "TacticTemplates",
                columns: table => new
                {
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    InternalKey = table.Column<string>(type: "TEXT", nullable: true),
                    TemplateIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    SettingsBlob = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacticTemplates", x => x.RecordOrdinal);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    InternalCode = table.Column<string>(type: "TEXT", nullable: true),
                    InternalCode2 = table.Column<string>(type: "TEXT", nullable: true),
                    Flag1 = table.Column<int>(type: "INTEGER", nullable: false),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", nullable: true),
                    NicknamePlacement = table.Column<int>(type: "INTEGER", nullable: false),
                    AffiliateParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    AffiliateParentId2 = table.Column<int>(type: "INTEGER", nullable: true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConferenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    DivisionId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    MarketSize = table.Column<int>(type: "INTEGER", nullable: false),
                    FanLoyalty = table.Column<int>(type: "INTEGER", nullable: false),
                    Finance1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Finance2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Finance3 = table.Column<int>(type: "INTEGER", nullable: false),
                    Finance4 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.TeamId);
                    table.ForeignKey(
                        name: "FK_Teams_Teams_AffiliateParentId",
                        column: x => x.AffiliateParentId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_Teams_AffiliateParentId2",
                        column: x => x.AffiliateParentId2,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NameListEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    NationIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    NameId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameListEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NameListEntries_Names_NameId",
                        column: x => x.NameId,
                        principalTable: "Names",
                        principalColumn: "NameId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TacticalRoleIndexEntries",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    List = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacticalRoleIndexEntries", x => new { x.RoleId, x.List, x.Ordinal });
                    table.CheckConstraint("CK_PlayerRoleIndexEntries_Value", "Value BETWEEN 0 AND 255");
                    table.ForeignKey(
                        name: "FK_TacticalRoleIndexEntries_TacticalRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "TacticalRoles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TacticalRoleWeights",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Group = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacticalRoleWeights", x => new { x.RoleId, x.Group, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_TacticalRoleWeights_TacticalRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "TacticalRoles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Personnel",
                columns: table => new
                {
                    PersonnelId = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstNameNameId = table.Column<int>(type: "INTEGER", nullable: false),
                    SurnameNameId = table.Column<int>(type: "INTEGER", nullable: false),
                    NicknameNameId = table.Column<int>(type: "INTEGER", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NationalityId = table.Column<int>(type: "INTEGER", nullable: false),
                    BirthCityId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    Job = table.Column<byte>(type: "INTEGER", nullable: false),
                    Negotiating = table.Column<int>(type: "INTEGER", nullable: true),
                    OffensivePreference = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PlayerManagement = table.Column<int>(type: "INTEGER", nullable: true),
                    PhysicalPreference = table.Column<ushort>(type: "INTEGER", nullable: false),
                    CoachingDefense = table.Column<int>(type: "INTEGER", nullable: true),
                    CoachingForwards = table.Column<int>(type: "INTEGER", nullable: true),
                    CoachingGoalies = table.Column<int>(type: "INTEGER", nullable: true),
                    CoachingProspects = table.Column<int>(type: "INTEGER", nullable: true),
                    EvaluateAbilities = table.Column<int>(type: "INTEGER", nullable: true),
                    EvaluatePotential = table.Column<int>(type: "INTEGER", nullable: true),
                    Reputation = table.Column<int>(type: "INTEGER", nullable: false),
                    LineMatchingTendency = table.Column<ushort>(type: "INTEGER", nullable: false),
                    GoalieHandlingTendency = table.Column<ushort>(type: "INTEGER", nullable: false),
                    VeteranPreference = table.Column<ushort>(type: "INTEGER", nullable: false),
                    InnovationTendency = table.Column<ushort>(type: "INTEGER", nullable: false),
                    LoyaltyTendency = table.Column<ushort>(type: "INTEGER", nullable: false),
                    Salary = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractLength = table.Column<int>(type: "INTEGER", nullable: true),
                    Retired = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefensiveSkills = table.Column<int>(type: "INTEGER", nullable: true),
                    OffensiveSkills = table.Column<int>(type: "INTEGER", nullable: true),
                    BasedInLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    PhysicalTraining = table.Column<int>(type: "INTEGER", nullable: true),
                    Tactics = table.Column<int>(type: "INTEGER", nullable: true),
                    Discipline = table.Column<int>(type: "INTEGER", nullable: true),
                    SelfPreservation = table.Column<int>(type: "INTEGER", nullable: true),
                    Motivation = table.Column<int>(type: "INTEGER", nullable: true),
                    IngameTactics = table.Column<int>(type: "INTEGER", nullable: true),
                    TrainerSkill = table.Column<int>(type: "INTEGER", nullable: true),
                    SerializedRecord = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnel", x => x.PersonnelId);
                    table.CheckConstraint("CK_Personnel_BasedInLocationId", "BasedInLocationId BETWEEN 0 AND 65535");
                    table.CheckConstraint("CK_Personnel_CoachingDefense", "CoachingDefense BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_CoachingForwards", "CoachingForwards BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_CoachingGoalies", "CoachingGoalies BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_CoachingProspects", "CoachingProspects BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_ContractLength", "ContractLength IS NULL OR ContractLength BETWEEN 1 AND 255");
                    table.CheckConstraint("CK_Personnel_DefensiveSkills", "DefensiveSkills BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_Discipline", "Discipline BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_EvaluateAbilities", "EvaluateAbilities BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_EvaluatePotential", "EvaluatePotential BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_GoalieHandlingTendency", "GoalieHandlingTendency BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Personnel_IngameTactics", "IngameTactics BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_InnovationTendency", "InnovationTendency BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Personnel_LineMatchingTendency", "LineMatchingTendency BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Personnel_LoyaltyTendency", "LoyaltyTendency BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Personnel_Motivation", "Motivation BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_NationalityId", "NationalityId BETWEEN 0 AND 65535");
                    table.CheckConstraint("CK_Personnel_Negotiating", "Negotiating BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_OffensiveSkills", "OffensiveSkills BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_PhysicalTraining", "PhysicalTraining BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_PlayerManagement", "PlayerManagement BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_Reputation", "Reputation BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Personnel_SelfPreservation", "SelfPreservation BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_Tactics", "Tactics BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_TrainerSkill", "TrainerSkill BETWEEN 0 AND 20");
                    table.CheckConstraint("CK_Personnel_VeteranPreference", "VeteranPreference BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_Personnel_Names_FirstNameNameId",
                        column: x => x.FirstNameNameId,
                        principalTable: "Names",
                        principalColumn: "NameId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Personnel_Names_NicknameNameId",
                        column: x => x.NicknameNameId,
                        principalTable: "Names",
                        principalColumn: "NameId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Personnel_Names_SurnameNameId",
                        column: x => x.SurnameNameId,
                        principalTable: "Names",
                        principalColumn: "NameId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Personnel_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    InternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstNameId = table.Column<int>(type: "INTEGER", nullable: true),
                    SurnameId = table.Column<int>(type: "INTEGER", nullable: true),
                    CommonNameId = table.Column<int>(type: "INTEGER", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    FranchiseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Goalie = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftDefense = table.Column<int>(type: "INTEGER", nullable: false),
                    RightDefense = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftWing = table.Column<int>(type: "INTEGER", nullable: false),
                    Center = table.Column<int>(type: "INTEGER", nullable: false),
                    RightWing = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimaryContractRole = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplementaryContractRole = table.Column<int>(type: "INTEGER", nullable: false),
                    SerializedRecord = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.InternalId);
                    table.ForeignKey(
                        name: "FK_Players_Names_CommonNameId",
                        column: x => x.CommonNameId,
                        principalTable: "Names",
                        principalColumn: "NameId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Players_Names_FirstNameId",
                        column: x => x.FirstNameId,
                        principalTable: "Names",
                        principalColumn: "NameId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Players_Names_SurnameId",
                        column: x => x.SurnameId,
                        principalTable: "Names",
                        principalColumn: "NameId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StoredLines",
                columns: table => new
                {
                    LineOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredLines", x => x.LineOrdinal);
                    table.ForeignKey(
                        name: "FK_StoredLines_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamTactics",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    SerializedSettings = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTactics", x => x.TeamId);
                    table.ForeignKey(
                        name: "FK_TeamTactics_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAttributes",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    BigGames = table.Column<int>(type: "INTEGER", nullable: false),
                    Consistency = table.Column<int>(type: "INTEGER", nullable: false),
                    Greed = table.Column<int>(type: "INTEGER", nullable: false),
                    Adaptability = table.Column<int>(type: "INTEGER", nullable: false),
                    Loyalty = table.Column<int>(type: "INTEGER", nullable: false),
                    Coachability = table.Column<int>(type: "INTEGER", nullable: false),
                    Aging = table.Column<int>(type: "INTEGER", nullable: false),
                    Sportsmanship = table.Column<int>(type: "INTEGER", nullable: false),
                    PassShootTendency = table.Column<int>(type: "INTEGER", nullable: false),
                    Controversy = table.Column<int>(type: "INTEGER", nullable: false),
                    HandleCritics = table.Column<int>(type: "INTEGER", nullable: false),
                    HandleFailure = table.Column<int>(type: "INTEGER", nullable: false),
                    HandleSuccess = table.Column<int>(type: "INTEGER", nullable: false),
                    Intelligence = table.Column<int>(type: "INTEGER", nullable: false),
                    Mood = table.Column<int>(type: "INTEGER", nullable: false),
                    DevelopmentRate = table.Column<int>(type: "INTEGER", nullable: false),
                    Aggression = table.Column<int>(type: "INTEGER", nullable: false),
                    Bravery = table.Column<int>(type: "INTEGER", nullable: false),
                    Determination = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamPlayer = table.Column<int>(type: "INTEGER", nullable: false),
                    Leadership = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperament = table.Column<int>(type: "INTEGER", nullable: false),
                    Professionalism = table.Column<int>(type: "INTEGER", nullable: false),
                    Ambition = table.Column<int>(type: "INTEGER", nullable: false),
                    Acceleration = table.Column<int>(type: "INTEGER", nullable: false),
                    Agility = table.Column<int>(type: "INTEGER", nullable: false),
                    Balance = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<int>(type: "INTEGER", nullable: false),
                    Stamina = table.Column<int>(type: "INTEGER", nullable: false),
                    Strength = table.Column<int>(type: "INTEGER", nullable: false),
                    Fighting = table.Column<int>(type: "INTEGER", nullable: false),
                    Reflexes = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieStamina = table.Column<int>(type: "INTEGER", nullable: false),
                    Screening = table.Column<int>(type: "INTEGER", nullable: false),
                    GettingOpen = table.Column<int>(type: "INTEGER", nullable: false),
                    Passing = table.Column<int>(type: "INTEGER", nullable: false),
                    Puckhandling = table.Column<int>(type: "INTEGER", nullable: false),
                    ShootingAccuracy = table.Column<int>(type: "INTEGER", nullable: false),
                    ShootingRange = table.Column<int>(type: "INTEGER", nullable: false),
                    OffensiveRead = table.Column<int>(type: "INTEGER", nullable: false),
                    Checking = table.Column<int>(type: "INTEGER", nullable: false),
                    Faceoffs = table.Column<int>(type: "INTEGER", nullable: false),
                    Hitting = table.Column<int>(type: "INTEGER", nullable: false),
                    Positioning = table.Column<int>(type: "INTEGER", nullable: false),
                    ShotBlocking = table.Column<int>(type: "INTEGER", nullable: false),
                    Stickchecking = table.Column<int>(type: "INTEGER", nullable: false),
                    DefensiveRead = table.Column<int>(type: "INTEGER", nullable: false),
                    GoaliePositioning = table.Column<int>(type: "INTEGER", nullable: false),
                    GoaliePassing = table.Column<int>(type: "INTEGER", nullable: false),
                    GoaliePokeCheck = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieBlocker = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieGlove = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieRebound = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieRecovery = table.Column<int>(type: "INTEGER", nullable: false),
                    GoaliePuckhandling = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieLowShots = table.Column<int>(type: "INTEGER", nullable: false),
                    MentalToughness = table.Column<int>(type: "INTEGER", nullable: false),
                    Skating = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAttributes", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_PlayerAttributes_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "InternalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerContracts",
                columns: table => new
                {
                    PlayerInternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractOrdinal = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerContracts", x => new { x.PlayerInternalId, x.ContractOrdinal });
                    table.ForeignKey(
                        name: "FK_PlayerContracts_Players_PlayerInternalId",
                        column: x => x.PlayerInternalId,
                        principalTable: "Players",
                        principalColumn: "InternalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTacticalRoleAssignments",
                columns: table => new
                {
                    PlayerInternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTacticalRoleAssignments", x => new { x.PlayerInternalId, x.Slot });
                    table.ForeignKey(
                        name: "FK_PlayerTacticalRoleAssignments_Players_PlayerInternalId",
                        column: x => x.PlayerInternalId,
                        principalTable: "Players",
                        principalColumn: "InternalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTacticalRoleAssignments_TacticalRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "TacticalRoles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamActiveLineSlots",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Group = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerInternalId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamActiveLineSlots", x => new { x.TeamId, x.Group, x.SlotOrdinal });
                    table.CheckConstraint("CK_TeamActiveLineSlots_Group", "\"Group\" >= 0 AND \"Group\" <= 12");
                    table.CheckConstraint("CK_TeamActiveLineSlots_SlotOrdinal", "SlotOrdinal >= 0");
                    table.ForeignKey(
                        name: "FK_TeamActiveLineSlots_Players_PlayerInternalId",
                        column: x => x.PlayerInternalId,
                        principalTable: "Players",
                        principalColumn: "InternalId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamActiveLineSlots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoredLineSlots",
                columns: table => new
                {
                    LineOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerInternalId = table.Column<int>(type: "INTEGER", nullable: true),
                    UnitLock = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredLineSlots", x => new { x.LineOrdinal, x.GroupOrdinal, x.SlotOrdinal });
                    table.ForeignKey(
                        name: "FK_StoredLineSlots_Players_PlayerInternalId",
                        column: x => x.PlayerInternalId,
                        principalTable: "Players",
                        principalColumn: "InternalId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoredLineSlots_StoredLines_LineOrdinal",
                        column: x => x.LineOrdinal,
                        principalTable: "StoredLines",
                        principalColumn: "LineOrdinal",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerContractYears",
                columns: table => new
                {
                    PlayerInternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    YearNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    MajorLeagueSalary = table.Column<int>(type: "INTEGER", nullable: true),
                    MinorLeagueSalary = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerContractYears", x => new { x.PlayerInternalId, x.ContractOrdinal, x.YearNumber });
                    table.CheckConstraint("CK_PlayerContractYears_MajorLeagueSalary", "MajorLeagueSalary IS NULL OR MajorLeagueSalary >= 0");
                    table.CheckConstraint("CK_PlayerContractYears_MinorLeagueSalary", "MinorLeagueSalary IS NULL OR MinorLeagueSalary >= 0");
                    table.CheckConstraint("CK_PlayerContractYears_YearNumber", "YearNumber BETWEEN 1 AND 14");
                    table.ForeignKey(
                        name: "FK_PlayerContractYears_PlayerContracts_PlayerInternalId_ContractOrdinal",
                        columns: x => new { x.PlayerInternalId, x.ContractOrdinal },
                        principalTable: "PlayerContracts",
                        principalColumns: new[] { "PlayerInternalId", "ContractOrdinal" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTacticalRoleTendencyValues",
                columns: table => new
                {
                    PlayerInternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Tendency = table.Column<int>(type: "INTEGER", nullable: false),
                    UseOverride = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTacticalRoleTendencyValues", x => new { x.PlayerInternalId, x.Slot, x.Tendency });
                    table.CheckConstraint("CK_PlayerRoleTendencyValues_UseOverride", "UseOverride BETWEEN 0 AND 255");
                    table.CheckConstraint("CK_PlayerRoleTendencyValues_Value", "Value BETWEEN 0 AND 65535");
                    table.ForeignKey(
                        name: "FK_PlayerTacticalRoleTendencyValues_PlayerTacticalRoleAssignments_PlayerInternalId_Slot",
                        columns: x => new { x.PlayerInternalId, x.Slot },
                        principalTable: "PlayerTacticalRoleAssignments",
                        principalColumns: new[] { "PlayerInternalId", "Slot" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NameListEntries_Kind_NationIndex_Ordinal",
                table: "NameListEntries",
                columns: new[] { "Kind", "NationIndex", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NameListEntries_NameId",
                table: "NameListEntries",
                column: "NameId");

            migrationBuilder.CreateIndex(
                name: "IX_Names_Ordinal",
                table: "Names",
                column: "Ordinal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NameScalars_Kind_Ordinal",
                table: "NameScalars",
                columns: new[] { "Kind", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_FirstNameNameId",
                table: "Personnel",
                column: "FirstNameNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_NicknameNameId",
                table: "Personnel",
                column: "NicknameNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_SurnameNameId",
                table: "Personnel",
                column: "SurnameNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_TeamId",
                table: "Personnel",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_CommonNameId",
                table: "Players",
                column: "CommonNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_FirstNameId",
                table: "Players",
                column: "FirstNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_RecordOrdinal",
                table: "Players",
                column: "RecordOrdinal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_SurnameId",
                table: "Players",
                column: "SurnameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTacticalRoleAssignments_RoleId",
                table: "PlayerTacticalRoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveFiles_RelativePath",
                table: "SaveFiles",
                column: "RelativePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredLines_TeamId",
                table: "StoredLines",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredLineSlots_PlayerInternalId",
                table: "StoredLineSlots",
                column: "PlayerInternalId");

            migrationBuilder.CreateIndex(
                name: "IX_TacticalRoles_RecordOrdinal",
                table: "TacticalRoles",
                column: "RecordOrdinal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamActiveLineSlots_PlayerInternalId",
                table: "TeamActiveLineSlots",
                column: "PlayerInternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_AffiliateParentId",
                table: "Teams",
                column: "AffiliateParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_AffiliateParentId2",
                table: "Teams",
                column: "AffiliateParentId2");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_RecordOrdinal",
                table: "Teams",
                column: "RecordOrdinal",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSettings");

            migrationBuilder.DropTable(
                name: "ModifierCatalogues");

            migrationBuilder.DropTable(
                name: "NameListEntries");

            migrationBuilder.DropTable(
                name: "NameScalars");

            migrationBuilder.DropTable(
                name: "Personnel");

            migrationBuilder.DropTable(
                name: "PlayerAttributes");

            migrationBuilder.DropTable(
                name: "PlayerContractYears");

            migrationBuilder.DropTable(
                name: "PlayerTacticalRoleTendencyValues");

            migrationBuilder.DropTable(
                name: "SaveFiles");

            migrationBuilder.DropTable(
                name: "SaveManifest");

            migrationBuilder.DropTable(
                name: "SetPlays");

            migrationBuilder.DropTable(
                name: "StoredLineSlots");

            migrationBuilder.DropTable(
                name: "TacticalRoleCatalogue");

            migrationBuilder.DropTable(
                name: "TacticalRoleIndexEntries");

            migrationBuilder.DropTable(
                name: "TacticalRoleWeights");

            migrationBuilder.DropTable(
                name: "TacticFiles");

            migrationBuilder.DropTable(
                name: "Tactics");

            migrationBuilder.DropTable(
                name: "TacticSystems");

            migrationBuilder.DropTable(
                name: "TacticTemplates");

            migrationBuilder.DropTable(
                name: "TeamActiveLineSlots");

            migrationBuilder.DropTable(
                name: "TeamTactics");

            migrationBuilder.DropTable(
                name: "PlayerContracts");

            migrationBuilder.DropTable(
                name: "PlayerTacticalRoleAssignments");

            migrationBuilder.DropTable(
                name: "StoredLines");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "TacticalRoles");

            migrationBuilder.DropTable(
                name: "Names");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
