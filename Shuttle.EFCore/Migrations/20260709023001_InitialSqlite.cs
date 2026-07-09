using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Abbreviation = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.LeagueId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DiscordId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => new { x.LeagueId, x.Season });
                    table.ForeignKey(
                        name: "FK_Seasons_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "LeagueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerInformation",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Handedness = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTpe = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliedTpe = table.Column<int>(type: "INTEGER", nullable: false),
                    BankedTpe = table.Column<int>(type: "INTEGER", nullable: false),
                    BankBalance = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    RetirementDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    JerseyNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Weight = table.Column<int>(type: "INTEGER", nullable: true),
                    Birthplace = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Recruiter = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Render = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    CurrentLeague = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentTeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    ShlRightsTeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    SmjhlRightsTeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    IihfNation = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    PositionChanged = table.Column<bool>(type: "INTEGER", nullable: false),
                    DraftSeason = table.Column<int>(type: "INTEGER", nullable: true),
                    UsedRedistribution = table.Column<int>(type: "INTEGER", nullable: false),
                    CoachingPurchased = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainingPurchased = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityCheckComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    TrainingCampComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSuspended = table.Column<bool>(type: "INTEGER", nullable: false),
                    Inactive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "'9999-12-31 23:59:59'"),
                    GoaltenderAttributes_Aggression = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Blocker = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Determination = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Glove = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Leadership = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_LowShots = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_MentalToughness = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Passing = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_PokeCheck = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Positioning = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Professionalism = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Puckhandling = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Rebound = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Recovery = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Reflexes = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Skating = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_Stamina = table.Column<int>(type: "INTEGER", nullable: true),
                    GoaltenderAttributes_TeamPlayer = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Acceleration = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Aggression = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Agility = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Balance = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Bravery = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Checking = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_DefensiveRead = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Determination = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Faceoffs = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Fighting = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_GettingOpen = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Hitting = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Leadership = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_OffensiveRead = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Passing = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Positioning = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Professionalism = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Puckhandling = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Screening = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_ShootingAccuracy = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_ShootingRange = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_ShotBlocking = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Speed = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Stamina = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Stickchecking = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Strength = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_TeamPlayer = table.Column<int>(type: "INTEGER", nullable: true),
                    SkaterAttributes_Temperament = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInformation", x => x.PlayerId);
                    table.UniqueConstraint("AK_PlayerInformation_PlayerId_UserId", x => new { x.PlayerId, x.UserId });
                    table.UniqueConstraint("AK_PlayerInformation_UserId_PlayerId", x => new { x.UserId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_PlayerInformation_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conferences",
                columns: table => new
                {
                    ConferenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conferences", x => new { x.ConferenceId, x.LeagueId, x.Season });
                    table.UniqueConstraint("AK_Conferences_ConferenceId_Season_LeagueId", x => new { x.ConferenceId, x.Season, x.LeagueId });
                    table.ForeignKey(
                        name: "FK_Conferences_Seasons_LeagueId_Season",
                        columns: x => new { x.LeagueId, x.Season },
                        principalTable: "Seasons",
                        principalColumns: new[] { "LeagueId", "Season" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndexRecords",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartSeason = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "'9999-12-31 23:59:59'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexRecords", x => new { x.PlayerId, x.LeagueId, x.StartSeason });
                    table.ForeignKey(
                        name: "FK_IndexRecords_PlayerInformation_PlayerId_UserId",
                        columns: x => new { x.PlayerId, x.UserId },
                        principalTable: "PlayerInformation",
                        principalColumns: new[] { "PlayerId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MostRecentUserPlayers",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MostRecentUserPlayers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_MostRecentUserPlayers_PlayerInformation_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerInformation",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    DivisionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConferenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => new { x.DivisionId, x.Season, x.LeagueId, x.ConferenceId });
                    table.UniqueConstraint("AK_Divisions_DivisionId_Season_LeagueId", x => new { x.DivisionId, x.Season, x.LeagueId });
                    table.ForeignKey(
                        name: "FK_Divisions_Conferences_ConferenceId_LeagueId_Season",
                        columns: x => new { x.ConferenceId, x.LeagueId, x.Season },
                        principalTable: "Conferences",
                        principalColumns: new[] { "ConferenceId", "LeagueId", "Season" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    DivisionId = table.Column<int>(type: "INTEGER", nullable: true),
                    ConferenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Abbreviation = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Stats_GoalsAgainst = table.Column<int>(type: "INTEGER", nullable: false),
                    Stats_GoalsFor = table.Column<int>(type: "INTEGER", nullable: false),
                    Stats_Losses = table.Column<int>(type: "INTEGER", nullable: false),
                    Stats_OvertimeLosses = table.Column<int>(type: "INTEGER", nullable: false),
                    Stats_Points = table.Column<int>(type: "INTEGER", nullable: false),
                    Stats_ShootoutLosses = table.Column<int>(type: "INTEGER", nullable: false),
                    Stats_ShootoutWins = table.Column<int>(type: "INTEGER", nullable: false),
                    Stats_WinPercent = table.Column<float>(type: "REAL", nullable: false),
                    Stats_Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Colors = table.Column<string>(type: "TEXT", nullable: false),
                    NameDetails = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => new { x.TeamId, x.Season, x.LeagueId });
                    table.UniqueConstraint("AK_Teams_Season_LeagueId_Abbreviation", x => new { x.Season, x.LeagueId, x.Abbreviation });
                    table.ForeignKey(
                        name: "FK_Teams_Conferences_ConferenceId_Season_LeagueId",
                        columns: x => new { x.ConferenceId, x.Season, x.LeagueId },
                        principalTable: "Conferences",
                        principalColumns: new[] { "ConferenceId", "Season", "LeagueId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Teams_Divisions_DivisionId_Season_LeagueId",
                        columns: x => new { x.DivisionId, x.Season, x.LeagueId },
                        principalTable: "Divisions",
                        principalColumns: new[] { "DivisionId", "Season", "LeagueId" });
                });

            migrationBuilder.CreateTable(
                name: "GameResults",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    SimDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HomeTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeScore = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayScore = table.Column<int>(type: "INTEGER", nullable: false),
                    Played = table.Column<bool>(type: "INTEGER", nullable: false),
                    Overtime = table.Column<bool>(type: "INTEGER", nullable: false),
                    Shootout = table.Column<bool>(type: "INTEGER", nullable: false),
                    GameType = table.Column<int>(type: "INTEGER", nullable: false),
                    DatePlayed = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameResults", x => x.Slug);
                    table.ForeignKey(
                        name: "FK_GameResults_Seasons_LeagueId_Season",
                        columns: x => new { x.LeagueId, x.Season },
                        principalTable: "Seasons",
                        principalColumns: new[] { "LeagueId", "Season" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameResults_Teams_AwayTeamId_Season_LeagueId",
                        columns: x => new { x.AwayTeamId, x.Season, x.LeagueId },
                        principalTable: "Teams",
                        principalColumns: new[] { "TeamId", "Season", "LeagueId" });
                    table.ForeignKey(
                        name: "FK_GameResults_Teams_HomeTeamId_Season_LeagueId",
                        columns: x => new { x.HomeTeamId, x.Season, x.LeagueId },
                        principalTable: "Teams",
                        principalColumns: new[] { "TeamId", "Season", "LeagueId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conferences_LeagueId_Season",
                table: "Conferences",
                columns: new[] { "LeagueId", "Season" });

            migrationBuilder.CreateIndex(
                name: "IX_Conferences_Name",
                table: "Conferences",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Conferences_Season",
                table: "Conferences",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_ConferenceId_LeagueId_Season",
                table: "Divisions",
                columns: new[] { "ConferenceId", "LeagueId", "Season" });

            migrationBuilder.CreateIndex(
                name: "IX_GameResults_AwayTeamId_Season_LeagueId",
                table: "GameResults",
                columns: new[] { "AwayTeamId", "Season", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_GameResults_HomeTeamId_Season_LeagueId",
                table: "GameResults",
                columns: new[] { "HomeTeamId", "Season", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_GameResults_LeagueId_GameId",
                table: "GameResults",
                columns: new[] { "LeagueId", "GameId" },
                unique: true,
                filter: "GameId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GameResults_LeagueId_Season",
                table: "GameResults",
                columns: new[] { "LeagueId", "Season" });

            migrationBuilder.CreateIndex(
                name: "IX_GameResults_LeagueId_SimDate",
                table: "GameResults",
                columns: new[] { "LeagueId", "SimDate" });

            migrationBuilder.CreateIndex(
                name: "IX_IndexRecords_IndexId",
                table: "IndexRecords",
                column: "IndexId");

            migrationBuilder.CreateIndex(
                name: "IX_IndexRecords_PlayerId_UserId",
                table: "IndexRecords",
                columns: new[] { "PlayerId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_IndexRecords_UserId",
                table: "IndexRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_Abbreviation",
                table: "Leagues",
                column: "Abbreviation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_Name",
                table: "Leagues",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MostRecentUserPlayers_PlayerId",
                table: "MostRecentUserPlayers",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInformation_CurrentTeamId",
                table: "PlayerInformation",
                column: "CurrentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInformation_DraftSeason",
                table: "PlayerInformation",
                column: "DraftSeason");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInformation_Name",
                table: "PlayerInformation",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInformation_UserId_CreationTime_PlayerId",
                table: "PlayerInformation",
                columns: new[] { "UserId", "CreationTime", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInformation_Username",
                table: "PlayerInformation",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_Season",
                table: "Seasons",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ConferenceId_Season_LeagueId",
                table: "Teams",
                columns: new[] { "ConferenceId", "Season", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DivisionId_Season_LeagueId",
                table: "Teams",
                columns: new[] { "DivisionId", "Season", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Location",
                table: "Teams",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameResults");

            migrationBuilder.DropTable(
                name: "IndexRecords");

            migrationBuilder.DropTable(
                name: "MostRecentUserPlayers");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "PlayerInformation");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Conferences");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "Leagues");
        }
    }
}
