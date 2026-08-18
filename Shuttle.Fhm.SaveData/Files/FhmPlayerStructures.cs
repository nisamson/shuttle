using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>Count-prefixed player position ratings and their paired unknown raw values.</summary>
public sealed class FhmPlayerPositionRatings
{
    public IList<ushort> RawValues { get; } = new List<ushort>([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
    public ushort Goalie { get => RawValues[0]; set => RawValues[0] = value; }
    public ushort UnknownGoalieRaw { get => RawValues[1]; set => RawValues[1] = value; }
    public ushort LeftDefenceman { get => RawValues[2]; set => RawValues[2] = value; }
    public ushort UnknownLeftDefencemanRaw { get => RawValues[3]; set => RawValues[3] = value; }
    public ushort RightDefenceman { get => RawValues[4]; set => RawValues[4] = value; }
    public ushort UnknownRightDefencemanRaw { get => RawValues[5]; set => RawValues[5] = value; }
    public ushort LeftWing { get => RawValues[6]; set => RawValues[6] = value; }
    public ushort UnknownLeftWingRaw { get => RawValues[7]; set => RawValues[7] = value; }
    public ushort Centre { get => RawValues[8]; set => RawValues[8] = value; }
    public ushort UnknownCentreRaw { get => RawValues[9]; set => RawValues[9] = value; }
    public ushort RightWing { get => RawValues[10]; set => RawValues[10] = value; }
    public ushort UnknownRightWingRaw { get => RawValues[11]; set => RawValues[11] = value; }
}

/// <summary>The complete 58-byte hidden and visible player-attribute vector.</summary>
public sealed class FhmPlayerAttributes
{
    public byte BigGames { get; set; }
    public byte Consistency { get; set; }
    public byte Greed { get; set; }
    public byte Adaptability { get; set; }
    public byte Loyalty { get; set; }
    public byte Coachability { get; set; }
    public byte Aging { get; set; }
    public byte Sportsmanship { get; set; }
    public byte PassShootTendency { get; set; }
    public byte Controversy { get; set; }
    public byte HandleCritics { get; set; }
    public byte HandleFailure { get; set; }
    public byte HandleSuccess { get; set; }
    public byte Intelligence { get; set; }
    public byte Mood { get; set; }
    public byte DevRate { get; set; }
    public byte Aggression { get; set; }
    public byte Bravery { get; set; }
    public byte Determination { get; set; }
    public byte Teamplayer { get; set; }
    public byte Leadership { get; set; }
    public byte Temperament { get; set; }
    public byte Professionalism { get; set; }
    public byte Ambition { get; set; }
    public byte Acceleration { get; set; }
    public byte Agility { get; set; }
    public byte Balance { get; set; }
    public byte Speed { get; set; }
    public byte Stamina { get; set; }
    public byte Strength { get; set; }
    public byte Fighting { get; set; }
    public byte GoalieReflexes { get; set; }
    public byte GoalieStamina { get; set; }
    public byte Screening { get; set; }
    public byte GettingOpen { get; set; }
    public byte Passing { get; set; }
    public byte PuckHandling { get; set; }
    public byte ShootingAccuracy { get; set; }
    public byte ShootingRange { get; set; }
    public byte OffensiveRead { get; set; }
    public byte Checking { get; set; }
    public byte Faceoffs { get; set; }
    public byte Hitting { get; set; }
    public byte Positioning { get; set; }
    public byte ShotBlocking { get; set; }
    public byte Stickchecking { get; set; }
    public byte DefensiveRead { get; set; }
    public byte GoaliePositioning { get; set; }
    public byte GoaliePassing { get; set; }
    public byte GoaliePokecheck { get; set; }
    public byte GoalieBlocker { get; set; }
    public byte GoalieGlove { get; set; }
    public byte GoalieRebound { get; set; }
    public byte GoalieRecovery { get; set; }
    public byte GoaliePuckhandling { get; set; }
    public byte GoalieLowShots { get; set; }
    public byte MentalToughness { get; set; }
    public byte GoalieSkating { get; set; }
}

/// <summary>A selected player tactical role and its nine override slots.</summary>
public sealed class FhmPlayerRoleInstance
{
    public int RoleId { get; set; }
    public IList<byte> UseOverride { get; } = new byte[9];
    public IList<ushort> TendencyValue { get; } = new ushort[9];
    public byte AttackingUseOverride { get => UseOverride[0]; set => UseOverride[0] = value; }
    public ushort AttackingTendencyValue { get => TendencyValue[0]; set => TendencyValue[0] = value; }
    public byte AggressivenessUseOverride { get => UseOverride[1]; set => UseOverride[1] = value; }
    public ushort AggressivenessTendencyValue { get => TendencyValue[1]; set => TendencyValue[1] = value; }
    public byte BackcheckingUseOverride { get => UseOverride[2]; set => UseOverride[2] = value; }
    public ushort BackcheckingTendencyValue { get => TendencyValue[2]; set => TendencyValue[2] = value; }
    public byte PressureUseOverride { get => UseOverride[3]; set => UseOverride[3] = value; }
    public ushort PressureTendencyValue { get => TendencyValue[3]; set => TendencyValue[3] = value; }
    public byte HittingUseOverride { get => UseOverride[4]; set => UseOverride[4] = value; }
    public ushort HittingTendencyValue { get => TendencyValue[4]; set => TendencyValue[4] = value; }
    public byte TempoUseOverride { get => UseOverride[5]; set => UseOverride[5] = value; }
    public ushort TempoTendencyValue { get => TendencyValue[5]; set => TendencyValue[5] = value; }
    public byte PassingUseOverride { get => UseOverride[6]; set => UseOverride[6] = value; }
    public ushort PassingTendencyValue { get => TendencyValue[6]; set => TendencyValue[6] = value; }
    public byte ShootingUseOverride { get => UseOverride[7]; set => UseOverride[7] = value; }
    public ushort ShootingTendencyValue { get => TendencyValue[7]; set => TendencyValue[7] = value; }
    public byte ReservedUseOverride { get => UseOverride[8]; set => UseOverride[8] = value; }
    public ushort ReservedTendencyValue { get => TendencyValue[8]; set => TendencyValue[8] = value; }
}

/// <summary>A structurally delineated player contract.</summary>
public sealed class FhmPlayerContract
{
    public IList<int> UnknownS4Values01 { get; } = new int[2];
    public IList<ushort> UnknownU2Values01 { get; } = new ushort[2];
    public int UnknownS401 { get; set; }
    public FhmDate UnknownDate01 { get; set; }
    public double UnknownF801 { get; set; }
    public IList<int> UnknownS4List { get; } = [];
    public IList<ushort> UnknownU2Values02 { get; } = new ushort[3];
    public int UnknownS402 { get; set; }
    public ushort UnknownU201 { get; set; }
    public byte UnknownU101 { get; set; }
    public IList<int> UnknownS4Values02 { get; } = new int[2];
    public ushort UnknownU202 { get; set; }
    public IList<byte> UnknownU1Values01 { get; } = new byte[2];
    public IList<int> UnknownS4Values03 { get; } = new int[4];
    public IList<byte> UnknownU1Values02 { get; } = new byte[4];
    public int UnknownS403 { get; set; }
    public IList<byte> UnknownU1Values03 { get; } = new byte[2];
    public int UnknownS404 { get; set; }
    public IList<byte> UnknownU1Values04 { get; } = new byte[3];
    public int UnknownS405 { get; set; }
    public ushort UnknownU203 { get; set; }
    public int UnknownS406 { get; set; }
    public IList<byte> UnknownU1Values05 { get; } = new byte[3];
    public IList<ushort> UnknownU2Values03 { get; } = new ushort[3];
    public IList<byte> UnknownU1Values06 { get; } = new byte[4];
    public IList<byte> UnknownU1List01 { get; } = [];
    public IList<byte> UnknownU1List02 { get; } = [];
    public IList<byte> UnknownU1Values07 { get; } = new byte[3];
}

/// <summary>One aggregate skater-statistics record.</summary>
public sealed class FhmAggregateSkaterStats
{
    public IList<ushort> HeaderU2 { get; } = new ushort[5];
    public int ValueS401 { get; set; }
    public IList<ushort> CountersU201 { get; } = new ushort[18];
    public IList<int> ValuesS401 { get; } = new int[3];
    public IList<ushort> CountersU202 { get; } = new ushort[7];
    public IList<double> ValuesF801 { get; } = new double[3];
    public ushort ValueU201 { get; set; }
    public IList<double> ValuesF802 { get; } = new double[3];
    public IList<ushort> ValuesU201 { get; } = new ushort[2];
    public IList<byte> FlagsU1 { get; } = new byte[3];
    public ushort Year { get => HeaderU2[0]; set => HeaderU2[0] = value; }
    public ushort TeamId { get => HeaderU2[1]; set => HeaderU2[1] = value; }
    public ushort LeagueId { get => HeaderU2[2]; set => HeaderU2[2] = value; }
    public FhmEnumValue<FhmGameType> GameType { get => new(HeaderU2[3]); set => HeaderU2[3] = value.RawValue; }
    public ushort UnknownHeaderU2 { get => HeaderU2[4]; set => HeaderU2[4] = value; }
    public int GamesPlayed { get => ValueS401; set => ValueS401 = value; }
    public ushort Goals { get => CountersU201[0]; set => CountersU201[0] = value; }
    public ushort Assists { get => CountersU201[1]; set => CountersU201[1] = value; }
    public ushort PenaltyMinutes { get => CountersU201[2]; set => CountersU201[2] = value; }
    public ushort PowerPlayGoals { get => CountersU201[3]; set => CountersU201[3] = value; }
    public ushort PowerPlayAssists { get => CountersU201[4]; set => CountersU201[4] = value; }
    public ushort ShortHandedGoals { get => CountersU201[5]; set => CountersU201[5] = value; }
    public ushort ShortHandedAssists { get => CountersU201[6]; set => CountersU201[6] = value; }
    public ushort GameWinningGoals { get => CountersU201[7]; set => CountersU201[7] = value; }
    public ushort FaceoffAttempts { get => CountersU201[8]; set => CountersU201[8] = value; }
    public ushort FaceoffWins { get => CountersU201[9]; set => CountersU201[9] = value; }
    public ushort Hits { get => CountersU201[10]; set => CountersU201[10] = value; }
    public ushort Giveaways { get => CountersU201[11]; set => CountersU201[11] = value; }
    public ushort Takeaways { get => CountersU201[12]; set => CountersU201[12] = value; }
    public ushort BlockedShots { get => CountersU201[13]; set => CountersU201[13] = value; }
    public ushort PlusMinusRaw { get => CountersU201[14]; set => CountersU201[14] = value; }
    public ushort ShotsOnGoal { get => CountersU201[15]; set => CountersU201[15] = value; }
    public int TimeOnIceMilliseconds { get => ValuesS401[0]; set => ValuesS401[0] = value; }
    public int PowerPlayTimeOnIceMilliseconds { get => ValuesS401[1]; set => ValuesS401[1] = value; }
    public int ShortHandedTimeOnIceMilliseconds { get => ValuesS401[2]; set => ValuesS401[2] = value; }
    public ushort Fights { get => CountersU202[0]; set => CountersU202[0] = value; }
    public ushort FightWins { get => CountersU202[1]; set => CountersU202[1] = value; }
}

/// <summary>One aggregate goalie-statistics record.</summary>
public sealed class FhmAggregateGoalieStats
{
    public IList<ushort> HeaderU2 { get; } = new ushort[5];
    public int ValueS401 { get; set; }
    public IList<ushort> ValuesU201 { get; } = new ushort[2];
    public int ValueS402 { get; set; }
    public IList<ushort> CountersU2 { get; } = new ushort[10];
    public double ValueF8 { get; set; }
    public ushort ValueU201 { get; set; }
    public byte FlagU1 { get; set; }
    public ushort Year { get => HeaderU2[0]; set => HeaderU2[0] = value; }
    public ushort TeamId { get => HeaderU2[1]; set => HeaderU2[1] = value; }
    public ushort LeagueId { get => HeaderU2[2]; set => HeaderU2[2] = value; }
    public FhmEnumValue<FhmGameType> GameType { get => new(HeaderU2[3]); set => HeaderU2[3] = value.RawValue; }
    public ushort UnknownHeaderU2 { get => HeaderU2[4]; set => HeaderU2[4] = value; }
    public int GamesPlayed { get => ValueS401; set => ValueS401 = value; }
    public int MinutesMilliseconds { get => ValueS402; set => ValueS402 = value; }
    public ushort Wins { get => CountersU2[0]; set => CountersU2[0] = value; }
    public ushort Losses { get => CountersU2[1]; set => CountersU2[1] = value; }
    public ushort TiesOrOvertimeLosses { get => CountersU2[2]; set => CountersU2[2] = value; }
    public ushort EmptyNetGoals { get => CountersU2[3]; set => CountersU2[3] = value; }
    public ushort Shutouts { get => CountersU2[4]; set => CountersU2[4] = value; }
    public ushort GoalsAgainst { get => CountersU2[5]; set => CountersU2[5] = value; }
    public ushort ShotsAgainst { get => CountersU2[6]; set => CountersU2[6] = value; }
    public double CumulativeGameRating { get => ValueF8; set => ValueF8 = value; }
}

/// <summary>One detailed skater game-statistics record.</summary>
public sealed class FhmDetailedSkaterGameStats
{
    public IList<ushort> HeaderU2 { get; } = new ushort[5];
    public int ValueS401 { get; set; }
    public IList<ushort> CountersU201 { get; } = new ushort[18];
    public IList<int> ValuesS401 { get; } = new int[3];
    public IList<ushort> CountersU202 { get; } = new ushort[26];
    public int ValueS402 { get; set; }
    public IList<double> ValuesF8 { get; } = new double[3];
    public IList<ushort> ValuesU201 { get; } = new ushort[6];
    public IList<byte> TrailingU1 { get; } = new byte[10];
    public ushort Year { get => HeaderU2[0]; set => HeaderU2[0] = value; }
    public ushort TeamId { get => HeaderU2[1]; set => HeaderU2[1] = value; }
    public ushort LeagueId { get => HeaderU2[2]; set => HeaderU2[2] = value; }
    public FhmEnumValue<FhmGameType> GameType { get => new(HeaderU2[3]); set => HeaderU2[3] = value.RawValue; }
    public ushort UnknownHeaderU2 { get => HeaderU2[4]; set => HeaderU2[4] = value; }
    public int UnknownS4RecordIdentifier { get => ValueS401; set => ValueS401 = value; }
    public ushort Goals { get => CountersU201[0]; set => CountersU201[0] = value; }
    public ushort Assists { get => CountersU201[1]; set => CountersU201[1] = value; }
    public ushort PenaltyMinutes { get => CountersU201[2]; set => CountersU201[2] = value; }
    public ushort PowerPlayGoals { get => CountersU201[3]; set => CountersU201[3] = value; }
    public ushort PowerPlayAssists { get => CountersU201[4]; set => CountersU201[4] = value; }
    public ushort ShortHandedGoals { get => CountersU201[5]; set => CountersU201[5] = value; }
    public ushort ShortHandedAssists { get => CountersU201[6]; set => CountersU201[6] = value; }
    public ushort GameWinningGoals { get => CountersU201[7]; set => CountersU201[7] = value; }
    public ushort FaceoffAttempts { get => CountersU201[8]; set => CountersU201[8] = value; }
    public ushort FaceoffWins { get => CountersU201[9]; set => CountersU201[9] = value; }
    public ushort Hits { get => CountersU201[10]; set => CountersU201[10] = value; }
    public ushort Giveaways { get => CountersU201[11]; set => CountersU201[11] = value; }
    public ushort Takeaways { get => CountersU201[12]; set => CountersU201[12] = value; }
    public ushort BlockedShots { get => CountersU201[13]; set => CountersU201[13] = value; }
    public ushort PlusMinusRaw { get => CountersU201[14]; set => CountersU201[14] = value; }
    public ushort ShotsOnGoal { get => CountersU201[15]; set => CountersU201[15] = value; }
    public int TimeOnIceMilliseconds { get => ValuesS401[0]; set => ValuesS401[0] = value; }
    public int PowerPlayTimeOnIceMilliseconds { get => ValuesS401[1]; set => ValuesS401[1] = value; }
    public int ShortHandedTimeOnIceMilliseconds { get => ValuesS401[2]; set => ValuesS401[2] = value; }
    public ushort Fights { get => CountersU202[0]; set => CountersU202[0] = value; }
    public ushort FightWins { get => CountersU202[1]; set => CountersU202[1] = value; }
    public ushort OnIceGoalsFor { get => CountersU202[2]; set => CountersU202[2] = value; }
    public ushort OnIceGoalsAgainst { get => CountersU202[3]; set => CountersU202[3] = value; }
    public ushort OnIceShotsFor { get => CountersU202[4]; set => CountersU202[4] = value; }
    public ushort OnIceShotsAgainst { get => CountersU202[5]; set => CountersU202[5] = value; }
    public ushort MissedShotsFor { get => CountersU202[6]; set => CountersU202[6] = value; }
    public ushort MissedShotsAgainst { get => CountersU202[7]; set => CountersU202[7] = value; }
    public ushort BlockedAttemptsFor { get => CountersU202[8]; set => CountersU202[8] = value; }
    public ushort BlockedAttemptsAgainst { get => CountersU202[9]; set => CountersU202[9] = value; }
}

/// <summary>One detailed goalie game-statistics record.</summary>
public sealed class FhmDetailedGoalieGameStats
{
    public IList<ushort> HeaderU2 { get; } = new ushort[5];
    public int ValueS401 { get; set; }
    public IList<ushort> ValuesU201 { get; } = new ushort[2];
    public int ValueS402 { get; set; }
    public IList<ushort> CountersU201 { get; } = new ushort[10];
    public IList<ushort> CountersU202 { get; } = new ushort[2];
    public double ValueF8 { get; set; }
    public IList<ushort> ValuesU202 { get; } = new ushort[6];
    public byte TrailingU1 { get; set; }
    public ushort Year { get => HeaderU2[0]; set => HeaderU2[0] = value; }
    public ushort TeamId { get => HeaderU2[1]; set => HeaderU2[1] = value; }
    public ushort LeagueId { get => HeaderU2[2]; set => HeaderU2[2] = value; }
    public FhmEnumValue<FhmGameType> GameType { get => new(HeaderU2[3]); set => HeaderU2[3] = value.RawValue; }
    public ushort UnknownHeaderU2 { get => HeaderU2[4]; set => HeaderU2[4] = value; }
    public int UnknownS4RecordIdentifier { get => ValueS401; set => ValueS401 = value; }
    public int MinutesMilliseconds { get => ValueS402; set => ValueS402 = value; }
    public ushort Wins { get => CountersU201[0]; set => CountersU201[0] = value; }
    public ushort Losses { get => CountersU201[1]; set => CountersU201[1] = value; }
    public ushort TiesOrOvertimeLosses { get => CountersU201[2]; set => CountersU201[2] = value; }
    public ushort EmptyNetGoals { get => CountersU201[3]; set => CountersU201[3] = value; }
    public ushort Shutouts { get => CountersU201[4]; set => CountersU201[4] = value; }
    public ushort GoalsAgainst { get => CountersU201[5]; set => CountersU201[5] = value; }
    public ushort ShotsAgainst { get => CountersU201[6]; set => CountersU201[6] = value; }
    public double CumulativeGameRating { get => ValueF8; set => ValueF8 = value; }
}

/// <summary>A Julian-date record with one string and structurally unknown values.</summary>
public sealed class FhmDatedStringRecord
{
    public long JulianDay { get; set; }
    public byte UnknownU101 { get; set; }
    public IList<int> UnknownS4Values01 { get; } = new int[3];
    public string? Text { get; set; }
    public IList<int> UnknownS4Values02 { get; } = new int[2];
}

/// <summary>A date with a fixed two-value unsigned vector.</summary>
public sealed class FhmDatedUshortRecord
{
    public FhmDate Date { get; set; }
    public IList<ushort> UnknownU2Values { get; } = new ushort[2];
}

/// <summary>A two-byte fixed record.</summary>
public sealed class FhmFixed2Record
{
    public byte Value01 { get; set; }
    public byte Value02 { get; set; }
}

/// <summary>An opaque fixed eight-byte record.</summary>
public sealed class FhmFixed8Record
{
    public FhmOpaqueBytes Data { get; set; } = new(new byte[8]);
}

/// <summary>An opaque fixed sixteen-byte record.</summary>
public sealed class FhmFixed16Record
{
    public FhmOpaqueBytes Data { get; set; } = new(new byte[16]);
}

/// <summary>An opaque fixed twenty-three-byte record.</summary>
public sealed class FhmFixed23Record
{
    public FhmOpaqueBytes Data { get; set; } = new(new byte[23]);
}

/// <summary>An opaque fixed twenty-four-byte record.</summary>
public sealed class FhmFixed24Record
{
    public FhmOpaqueBytes Data { get; set; } = new(new byte[24]);
}

/// <summary>A structurally known byte/signed-value pair.</summary>
public sealed class FhmByteInt32Pair
{
    public byte UnknownU1 { get; set; }
    public int UnknownS4 { get; set; }
}
