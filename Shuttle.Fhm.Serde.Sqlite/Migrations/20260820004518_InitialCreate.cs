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
                    ValueKind = table.Column<string>(type: "TEXT", nullable: false),
                    IntegerValue = table.Column<long>(type: "INTEGER", nullable: true),
                    RealValue = table.Column<double>(type: "REAL", nullable: true),
                    TextValue = table.Column<string>(type: "TEXT", nullable: true)
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
                    CollectionKind = table.Column<string>(type: "TEXT", nullable: false),
                    NationIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    NameId = table.Column<int>(type: "INTEGER", nullable: false),
                    IntegerValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryWeight = table.Column<int>(type: "INTEGER", nullable: false),
                    FlagA = table.Column<int>(type: "INTEGER", nullable: false),
                    FlagB = table.Column<int>(type: "INTEGER", nullable: false),
                    FlagC = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Names", x => new { x.CollectionKind, x.NationIndex, x.Ordinal });
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    InternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstNameId = table.Column<int>(type: "INTEGER", nullable: false),
                    SurnameId = table.Column<int>(type: "INTEGER", nullable: false),
                    CommonNameId = table.Column<int>(type: "INTEGER", nullable: false),
                    BirthYear = table.Column<int>(type: "INTEGER", nullable: false),
                    BirthMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    BirthDay = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    FranchiseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Goalie = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftDefenceman = table.Column<int>(type: "INTEGER", nullable: false),
                    RightDefenceman = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftWing = table.Column<int>(type: "INTEGER", nullable: false),
                    Centre = table.Column<int>(type: "INTEGER", nullable: false),
                    RightWing = table.Column<int>(type: "INTEGER", nullable: false),
                    SerializedRecord = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.RecordOrdinal);
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
                name: "StoredLines",
                columns: table => new
                {
                    LineOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredLines", x => x.LineOrdinal);
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
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    InternalCode = table.Column<string>(type: "TEXT", nullable: true),
                    InternalCode2 = table.Column<string>(type: "TEXT", nullable: true),
                    Flag1 = table.Column<int>(type: "INTEGER", nullable: false),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", nullable: true),
                    NicknamePlacement = table.Column<int>(type: "INTEGER", nullable: false),
                    AffiliateParentId = table.Column<int>(type: "INTEGER", nullable: false),
                    AffiliateParentId2 = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Teams", x => x.RecordOrdinal);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAttributes",
                columns: table => new
                {
                    RecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
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
                    DevRate = table.Column<int>(type: "INTEGER", nullable: false),
                    Aggression = table.Column<int>(type: "INTEGER", nullable: false),
                    Bravery = table.Column<int>(type: "INTEGER", nullable: false),
                    Determination = table.Column<int>(type: "INTEGER", nullable: false),
                    Teamplayer = table.Column<int>(type: "INTEGER", nullable: false),
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
                    GoalieReflexes = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieStamina = table.Column<int>(type: "INTEGER", nullable: false),
                    Screening = table.Column<int>(type: "INTEGER", nullable: false),
                    GettingOpen = table.Column<int>(type: "INTEGER", nullable: false),
                    Passing = table.Column<int>(type: "INTEGER", nullable: false),
                    PuckHandling = table.Column<int>(type: "INTEGER", nullable: false),
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
                    GoaliePokecheck = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieBlocker = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieGlove = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieRebound = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieRecovery = table.Column<int>(type: "INTEGER", nullable: false),
                    GoaliePuckhandling = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieLowShots = table.Column<int>(type: "INTEGER", nullable: false),
                    MentalToughness = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalieSkating = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAttributes", x => x.RecordOrdinal);
                    table.ForeignKey(
                        name: "FK_PlayerAttributes_Players_RecordOrdinal",
                        column: x => x.RecordOrdinal,
                        principalTable: "Players",
                        principalColumn: "RecordOrdinal",
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
                        name: "FK_StoredLineSlots_StoredLines_LineOrdinal",
                        column: x => x.LineOrdinal,
                        principalTable: "StoredLines",
                        principalColumn: "LineOrdinal",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamTactics",
                columns: table => new
                {
                    TeamRecordOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    SerializedSettings = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTactics", x => x.TeamRecordOrdinal);
                    table.ForeignKey(
                        name: "FK_TeamTactics_Teams_TeamRecordOrdinal",
                        column: x => x.TeamRecordOrdinal,
                        principalTable: "Teams",
                        principalColumn: "RecordOrdinal",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaveFiles_RelativePath",
                table: "SaveFiles",
                column: "RelativePath",
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
                name: "Names");

            migrationBuilder.DropTable(
                name: "PlayerAttributes");

            migrationBuilder.DropTable(
                name: "SaveFiles");

            migrationBuilder.DropTable(
                name: "SaveManifest");

            migrationBuilder.DropTable(
                name: "SetPlays");

            migrationBuilder.DropTable(
                name: "StoredLineSlots");

            migrationBuilder.DropTable(
                name: "TacticFiles");

            migrationBuilder.DropTable(
                name: "Tactics");

            migrationBuilder.DropTable(
                name: "TacticSystems");

            migrationBuilder.DropTable(
                name: "TacticTemplates");

            migrationBuilder.DropTable(
                name: "TeamTactics");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "StoredLines");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
