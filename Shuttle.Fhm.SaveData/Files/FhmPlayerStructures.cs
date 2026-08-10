using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>Count-prefixed player position ratings and their paired unknown raw values.</summary>
public sealed class FhmPlayerPositionRatings
{
    /// <summary>Gets raw values in wire order, including the unknown odd-indexed values.</summary>
    public IList<ushort> RawValues { get; } = new List<ushort>([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

    /// <summary>Gets or sets the goalie rating.</summary>
    public ushort Goalie { get => RawValues[0]; set => RawValues[0] = value; }

    /// <summary>Gets or sets the raw value paired with the goalie rating.</summary>
    public ushort UnknownGoalieRaw { get => RawValues[1]; set => RawValues[1] = value; }

    /// <summary>Gets or sets the left-defenceman rating.</summary>
    public ushort LeftDefenceman { get => RawValues[2]; set => RawValues[2] = value; }

    /// <summary>Gets or sets the raw value paired with the left-defenceman rating.</summary>
    public ushort UnknownLeftDefencemanRaw { get => RawValues[3]; set => RawValues[3] = value; }

    /// <summary>Gets or sets the right-defenceman rating.</summary>
    public ushort RightDefenceman { get => RawValues[4]; set => RawValues[4] = value; }

    /// <summary>Gets or sets the raw value paired with the right-defenceman rating.</summary>
    public ushort UnknownRightDefencemanRaw { get => RawValues[5]; set => RawValues[5] = value; }

    /// <summary>Gets or sets the left-wing rating.</summary>
    public ushort LeftWing { get => RawValues[6]; set => RawValues[6] = value; }

    /// <summary>Gets or sets the raw value paired with the left-wing rating.</summary>
    public ushort UnknownLeftWingRaw { get => RawValues[7]; set => RawValues[7] = value; }

    /// <summary>Gets or sets the centre rating.</summary>
    public ushort Centre { get => RawValues[8]; set => RawValues[8] = value; }

    /// <summary>Gets or sets the raw value paired with the centre rating.</summary>
    public ushort UnknownCentreRaw { get => RawValues[9]; set => RawValues[9] = value; }

    /// <summary>Gets or sets the right-wing rating.</summary>
    public ushort RightWing { get => RawValues[10]; set => RawValues[10] = value; }

    /// <summary>Gets or sets the raw value paired with the right-wing rating.</summary>
    public ushort UnknownRightWingRaw { get => RawValues[11]; set => RawValues[11] = value; }

    internal void ReadFrom(FhmBinaryReader reader)
    {
        RawValues.Clear();
        var count = reader.ReadCount("player position ratings");
        for (var index = 0; index < count; index++)
        {
            RawValues.Add(reader.ReadUInt16());
        }
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteCount(RawValues.Count, "player position ratings");
        foreach (var value in RawValues)
        {
            writer.WriteUInt16(value);
        }
    }
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

    internal void ReadFrom(FhmBinaryReader reader)
    {
        BigGames = reader.ReadByte();
        Consistency = reader.ReadByte();
        Greed = reader.ReadByte();
        Adaptability = reader.ReadByte();
        Loyalty = reader.ReadByte();
        Coachability = reader.ReadByte();
        Aging = reader.ReadByte();
        Sportsmanship = reader.ReadByte();
        PassShootTendency = reader.ReadByte();
        Controversy = reader.ReadByte();
        HandleCritics = reader.ReadByte();
        HandleFailure = reader.ReadByte();
        HandleSuccess = reader.ReadByte();
        Intelligence = reader.ReadByte();
        Mood = reader.ReadByte();
        DevRate = reader.ReadByte();
        Aggression = reader.ReadByte();
        Bravery = reader.ReadByte();
        Determination = reader.ReadByte();
        Teamplayer = reader.ReadByte();
        Leadership = reader.ReadByte();
        Temperament = reader.ReadByte();
        Professionalism = reader.ReadByte();
        Ambition = reader.ReadByte();
        Acceleration = reader.ReadByte();
        Agility = reader.ReadByte();
        Balance = reader.ReadByte();
        Speed = reader.ReadByte();
        Stamina = reader.ReadByte();
        Strength = reader.ReadByte();
        Fighting = reader.ReadByte();
        GoalieReflexes = reader.ReadByte();
        GoalieStamina = reader.ReadByte();
        Screening = reader.ReadByte();
        GettingOpen = reader.ReadByte();
        Passing = reader.ReadByte();
        PuckHandling = reader.ReadByte();
        ShootingAccuracy = reader.ReadByte();
        ShootingRange = reader.ReadByte();
        OffensiveRead = reader.ReadByte();
        Checking = reader.ReadByte();
        Faceoffs = reader.ReadByte();
        Hitting = reader.ReadByte();
        Positioning = reader.ReadByte();
        ShotBlocking = reader.ReadByte();
        Stickchecking = reader.ReadByte();
        DefensiveRead = reader.ReadByte();
        GoaliePositioning = reader.ReadByte();
        GoaliePassing = reader.ReadByte();
        GoaliePokecheck = reader.ReadByte();
        GoalieBlocker = reader.ReadByte();
        GoalieGlove = reader.ReadByte();
        GoalieRebound = reader.ReadByte();
        GoalieRecovery = reader.ReadByte();
        GoaliePuckhandling = reader.ReadByte();
        GoalieLowShots = reader.ReadByte();
        MentalToughness = reader.ReadByte();
        GoalieSkating = reader.ReadByte();
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteByte(BigGames);
        writer.WriteByte(Consistency);
        writer.WriteByte(Greed);
        writer.WriteByte(Adaptability);
        writer.WriteByte(Loyalty);
        writer.WriteByte(Coachability);
        writer.WriteByte(Aging);
        writer.WriteByte(Sportsmanship);
        writer.WriteByte(PassShootTendency);
        writer.WriteByte(Controversy);
        writer.WriteByte(HandleCritics);
        writer.WriteByte(HandleFailure);
        writer.WriteByte(HandleSuccess);
        writer.WriteByte(Intelligence);
        writer.WriteByte(Mood);
        writer.WriteByte(DevRate);
        writer.WriteByte(Aggression);
        writer.WriteByte(Bravery);
        writer.WriteByte(Determination);
        writer.WriteByte(Teamplayer);
        writer.WriteByte(Leadership);
        writer.WriteByte(Temperament);
        writer.WriteByte(Professionalism);
        writer.WriteByte(Ambition);
        writer.WriteByte(Acceleration);
        writer.WriteByte(Agility);
        writer.WriteByte(Balance);
        writer.WriteByte(Speed);
        writer.WriteByte(Stamina);
        writer.WriteByte(Strength);
        writer.WriteByte(Fighting);
        writer.WriteByte(GoalieReflexes);
        writer.WriteByte(GoalieStamina);
        writer.WriteByte(Screening);
        writer.WriteByte(GettingOpen);
        writer.WriteByte(Passing);
        writer.WriteByte(PuckHandling);
        writer.WriteByte(ShootingAccuracy);
        writer.WriteByte(ShootingRange);
        writer.WriteByte(OffensiveRead);
        writer.WriteByte(Checking);
        writer.WriteByte(Faceoffs);
        writer.WriteByte(Hitting);
        writer.WriteByte(Positioning);
        writer.WriteByte(ShotBlocking);
        writer.WriteByte(Stickchecking);
        writer.WriteByte(DefensiveRead);
        writer.WriteByte(GoaliePositioning);
        writer.WriteByte(GoaliePassing);
        writer.WriteByte(GoaliePokecheck);
        writer.WriteByte(GoalieBlocker);
        writer.WriteByte(GoalieGlove);
        writer.WriteByte(GoalieRebound);
        writer.WriteByte(GoalieRecovery);
        writer.WriteByte(GoaliePuckhandling);
        writer.WriteByte(GoalieLowShots);
        writer.WriteByte(MentalToughness);
        writer.WriteByte(GoalieSkating);
    }
}

/// <summary>A selected player tactical role and its nine override slots.</summary>
public sealed class FhmPlayerRoleInstance
{
    /// <summary>Gets or sets the role id in <c>player_roles.dat</c>.</summary>
    public int RoleId { get; set; }

    /// <summary>Gets override flags in UI order: attacking through shooting, then reserved.</summary>
    public IList<byte> UseOverride { get; } = new byte[9];

    /// <summary>Gets override values in UI order: attacking through shooting, then reserved.</summary>
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

    internal static FhmPlayerRoleInstance? ReadOptional(FhmBinaryReader reader)
    {
        var presence = reader.ReadInt32();
        return presence switch
        {
            -1 => null,
            0 => Read(reader),
            _ => throw new FhmFormatException($"Invalid player role presence {presence} at offset {reader.Position - sizeof(int)}."),
        };
    }

    internal static void WriteOptional(FhmBinaryWriter writer, FhmPlayerRoleInstance? value)
    {
        writer.WriteInt32(value is null ? -1 : 0);
        value?.WriteTo(writer);
    }

    private static FhmPlayerRoleInstance Read(FhmBinaryReader reader)
    {
        var result = new FhmPlayerRoleInstance { RoleId = reader.ReadInt32() };
        FhmPlayerSerialization.ReadFixed(reader, result.UseOverride);
        FhmPlayerSerialization.ReadFixed(reader, result.TendencyValue);
        return result;
    }

    private void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(RoleId);
        FhmPlayerSerialization.WriteFixed(writer, UseOverride, 9, "player role override flags");
        FhmPlayerSerialization.WriteFixed(writer, TendencyValue, 9, "player role override values");
    }
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

    internal static FhmPlayerContract Read(FhmBinaryReader reader)
    {
        var result = new FhmPlayerContract();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownS4Values01);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values01);
        result.UnknownS401 = reader.ReadInt32();
        result.UnknownDate01 = reader.ReadDate();
        result.UnknownF801 = reader.ReadDouble();
        FhmPlayerSerialization.ReadInt32List(reader, result.UnknownS4List, "contract unknown signed list");
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values02);
        result.UnknownS402 = reader.ReadInt32();
        result.UnknownU201 = reader.ReadUInt16();
        result.UnknownU101 = reader.ReadByte();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownS4Values02);
        result.UnknownU202 = reader.ReadUInt16();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values01);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownS4Values03);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values02);
        result.UnknownS403 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values03);
        result.UnknownS404 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values04);
        result.UnknownS405 = reader.ReadInt32();
        result.UnknownU203 = reader.ReadUInt16();
        result.UnknownS406 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values05);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values03);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values06);
        FhmPlayerSerialization.ReadByteList(reader, result.UnknownU1List01, "contract unknown byte list 01");
        FhmPlayerSerialization.ReadByteList(reader, result.UnknownU1List02, "contract unknown byte list 02");
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values07);
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values01, 2, "contract UnknownS4Values01");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values01, 2, "contract UnknownU2Values01");
        writer.WriteInt32(UnknownS401);
        writer.WriteDate(UnknownDate01);
        writer.WriteDouble(UnknownF801);
        FhmPlayerSerialization.WriteInt32List(writer, UnknownS4List, "contract unknown signed list");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values02, 3, "contract UnknownU2Values02");
        writer.WriteInt32(UnknownS402);
        writer.WriteUInt16(UnknownU201);
        writer.WriteByte(UnknownU101);
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values02, 2, "contract UnknownS4Values02");
        writer.WriteUInt16(UnknownU202);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values01, 2, "contract UnknownU1Values01");
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values03, 4, "contract UnknownS4Values03");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values02, 4, "contract UnknownU1Values02");
        writer.WriteInt32(UnknownS403);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values03, 2, "contract UnknownU1Values03");
        writer.WriteInt32(UnknownS404);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values04, 3, "contract UnknownU1Values04");
        writer.WriteInt32(UnknownS405);
        writer.WriteUInt16(UnknownU203);
        writer.WriteInt32(UnknownS406);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values05, 3, "contract UnknownU1Values05");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values03, 3, "contract UnknownU2Values03");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values06, 4, "contract UnknownU1Values06");
        FhmPlayerSerialization.WriteByteList(writer, UnknownU1List01, "contract unknown byte list 01");
        FhmPlayerSerialization.WriteByteList(writer, UnknownU1List02, "contract unknown byte list 02");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values07, 3, "contract UnknownU1Values07");
    }
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

    internal static FhmAggregateSkaterStats Read(FhmBinaryReader reader)
    {
        var result = new FhmAggregateSkaterStats { ValueS401 = 0 };
        FhmPlayerSerialization.ReadFixed(reader, result.HeaderU2);
        result.ValueS401 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, result.CountersU201);
        FhmPlayerSerialization.ReadFixed(reader, result.ValuesS401);
        FhmPlayerSerialization.ReadFixed(reader, result.CountersU202);
        FhmPlayerSerialization.ReadFixed(reader, result.ValuesF801);
        result.ValueU201 = reader.ReadUInt16();
        FhmPlayerSerialization.ReadFixed(reader, result.ValuesF802);
        FhmPlayerSerialization.ReadFixed(reader, result.ValuesU201);
        FhmPlayerSerialization.ReadFixed(reader, result.FlagsU1);
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmPlayerSerialization.WriteFixed(writer, HeaderU2, 5, "aggregate skater HeaderU2");
        writer.WriteInt32(ValueS401);
        FhmPlayerSerialization.WriteFixed(writer, CountersU201, 18, "aggregate skater CountersU201");
        FhmPlayerSerialization.WriteFixed(writer, ValuesS401, 3, "aggregate skater ValuesS401");
        FhmPlayerSerialization.WriteFixed(writer, CountersU202, 7, "aggregate skater CountersU202");
        FhmPlayerSerialization.WriteFixed(writer, ValuesF801, 3, "aggregate skater ValuesF801");
        writer.WriteUInt16(ValueU201);
        FhmPlayerSerialization.WriteFixed(writer, ValuesF802, 3, "aggregate skater ValuesF802");
        FhmPlayerSerialization.WriteFixed(writer, ValuesU201, 2, "aggregate skater ValuesU201");
        FhmPlayerSerialization.WriteFixed(writer, FlagsU1, 3, "aggregate skater FlagsU1");
    }
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

        internal static FhmAggregateGoalieStats Read(FhmBinaryReader reader)
        {
            var result = new FhmAggregateGoalieStats();
            FhmPlayerSerialization.ReadFixed(reader, result.HeaderU2);
            result.ValueS401 = reader.ReadInt32();
            FhmPlayerSerialization.ReadFixed(reader, result.ValuesU201);
            result.ValueS402 = reader.ReadInt32();
            FhmPlayerSerialization.ReadFixed(reader, result.CountersU2);
            result.ValueF8 = reader.ReadDouble();
            result.ValueU201 = reader.ReadUInt16();
            result.FlagU1 = reader.ReadByte();
            return result;
        }

        internal void WriteTo(FhmBinaryWriter writer)
        {
            FhmPlayerSerialization.WriteFixed(writer, HeaderU2, 5, "aggregate goalie HeaderU2");
            writer.WriteInt32(ValueS401);
            FhmPlayerSerialization.WriteFixed(writer, ValuesU201, 2, "aggregate goalie ValuesU201");
            writer.WriteInt32(ValueS402);
            FhmPlayerSerialization.WriteFixed(writer, CountersU2, 10, "aggregate goalie CountersU2");
            writer.WriteDouble(ValueF8);
            writer.WriteUInt16(ValueU201);
            writer.WriteByte(FlagU1);
        }
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

        internal static FhmDetailedSkaterGameStats Read(FhmBinaryReader reader)
        {
            var result = new FhmDetailedSkaterGameStats();
            FhmPlayerSerialization.ReadFixed(reader, result.HeaderU2);
            result.ValueS401 = reader.ReadInt32();
            FhmPlayerSerialization.ReadFixed(reader, result.CountersU201);
            FhmPlayerSerialization.ReadFixed(reader, result.ValuesS401);
            FhmPlayerSerialization.ReadFixed(reader, result.CountersU202);
            result.ValueS402 = reader.ReadInt32();
            FhmPlayerSerialization.ReadFixed(reader, result.ValuesF8);
            FhmPlayerSerialization.ReadFixed(reader, result.ValuesU201);
            FhmPlayerSerialization.ReadFixed(reader, result.TrailingU1);
            return result;
        }

        internal void WriteTo(FhmBinaryWriter writer)
        {
            FhmPlayerSerialization.WriteFixed(writer, HeaderU2, 5, "detailed skater HeaderU2");
            writer.WriteInt32(ValueS401);
            FhmPlayerSerialization.WriteFixed(writer, CountersU201, 18, "detailed skater CountersU201");
            FhmPlayerSerialization.WriteFixed(writer, ValuesS401, 3, "detailed skater ValuesS401");
            FhmPlayerSerialization.WriteFixed(writer, CountersU202, 26, "detailed skater CountersU202");
            writer.WriteInt32(ValueS402);
            FhmPlayerSerialization.WriteFixed(writer, ValuesF8, 3, "detailed skater ValuesF8");
            FhmPlayerSerialization.WriteFixed(writer, ValuesU201, 6, "detailed skater ValuesU201");
            FhmPlayerSerialization.WriteFixed(writer, TrailingU1, 10, "detailed skater TrailingU1");
        }
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

        internal static FhmDetailedGoalieGameStats Read(FhmBinaryReader reader)
        {
            var result = new FhmDetailedGoalieGameStats();
            FhmPlayerSerialization.ReadFixed(reader, result.HeaderU2);
            result.ValueS401 = reader.ReadInt32();
            FhmPlayerSerialization.ReadFixed(reader, result.ValuesU201);
            result.ValueS402 = reader.ReadInt32();
            FhmPlayerSerialization.ReadFixed(reader, result.CountersU201);
            FhmPlayerSerialization.ReadFixed(reader, result.CountersU202);
            result.ValueF8 = reader.ReadDouble();
            FhmPlayerSerialization.ReadFixed(reader, result.ValuesU202);
            result.TrailingU1 = reader.ReadByte();
            return result;
        }

        internal void WriteTo(FhmBinaryWriter writer)
        {
            FhmPlayerSerialization.WriteFixed(writer, HeaderU2, 5, "detailed goalie HeaderU2");
            writer.WriteInt32(ValueS401);
            FhmPlayerSerialization.WriteFixed(writer, ValuesU201, 2, "detailed goalie ValuesU201");
            writer.WriteInt32(ValueS402);
            FhmPlayerSerialization.WriteFixed(writer, CountersU201, 10, "detailed goalie CountersU201");
            FhmPlayerSerialization.WriteFixed(writer, CountersU202, 2, "detailed goalie CountersU202");
            writer.WriteDouble(ValueF8);
            FhmPlayerSerialization.WriteFixed(writer, ValuesU202, 6, "detailed goalie ValuesU202");
            writer.WriteByte(TrailingU1);
        }
}

/// <summary>A Julian-date record with one string and structurally unknown values.</summary>
public sealed class FhmDatedStringRecord
        {
            public long JulianDay { get; set; }
            public byte UnknownU101 { get; set; }
            public IList<int> UnknownS4Values01 { get; } = new int[3];
            public string? Text { get; set; }
            public IList<int> UnknownS4Values02 { get; } = new int[2];

            internal static FhmDatedStringRecord Read(FhmBinaryReader reader)
            {
                var result = new FhmDatedStringRecord
                {
                    JulianDay = reader.ReadInt64(),
                    UnknownU101 = reader.ReadByte(),
                };
                FhmPlayerSerialization.ReadFixed(reader, result.UnknownS4Values01);
                result.Text = reader.ReadQString();
                FhmPlayerSerialization.ReadFixed(reader, result.UnknownS4Values02);
                return result;
            }

            internal void WriteTo(FhmBinaryWriter writer)
            {
                writer.WriteInt64(JulianDay);
                writer.WriteByte(UnknownU101);
                FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values01, 3, "dated string UnknownS4Values01");
                writer.WriteQString(Text);
                FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values02, 2, "dated string UnknownS4Values02");
            }
        }

        /// <summary>A date with a fixed two-value unsigned vector.</summary>
        public sealed class FhmDatedUshortRecord
        {
            public FhmDate Date { get; set; }
            public IList<ushort> UnknownU2Values { get; } = new ushort[2];

            internal static FhmDatedUshortRecord Read(FhmBinaryReader reader)
            {
                var result = new FhmDatedUshortRecord { Date = reader.ReadDate() };
                FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values);
                return result;
            }

            internal void WriteTo(FhmBinaryWriter writer)
            {
                writer.WriteDate(Date);
                FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values, 2, "dated unsigned UnknownU2Values");
            }
        }

        /// <summary>A two-byte fixed record.</summary>
        public sealed class FhmFixed2Record
        {
            public byte Value01 { get; set; }
            public byte Value02 { get; set; }

            internal static FhmFixed2Record Read(FhmBinaryReader reader) =>
                new() { Value01 = reader.ReadByte(), Value02 = reader.ReadByte() };

            internal void WriteTo(FhmBinaryWriter writer)
            {
                writer.WriteByte(Value01);
                writer.WriteByte(Value02);
            }
        }

        /// <summary>An opaque fixed eight-byte record whose wire size is documented.</summary>
        public sealed class FhmFixed8Record
        {
            public FhmOpaqueBytes Data { get; set; } = new(new byte[8]);

            internal static FhmFixed8Record Read(FhmBinaryReader reader) => new() { Data = reader.ReadOpaqueBytes(8) };

            internal void WriteTo(FhmBinaryWriter writer) => FhmPlayerSerialization.WriteOpaqueFixed(writer, Data, 8, "fixed-8 record");
        }

        /// <summary>An opaque fixed 16-byte record whose wire size is documented.</summary>
        public sealed class FhmFixed16Record
        {
            public FhmOpaqueBytes Data { get; set; } = new(new byte[16]);

            internal static FhmFixed16Record Read(FhmBinaryReader reader) => new() { Data = reader.ReadOpaqueBytes(16) };

            internal void WriteTo(FhmBinaryWriter writer) => FhmPlayerSerialization.WriteOpaqueFixed(writer, Data, 16, "fixed-16 record");
        }

        /// <summary>An opaque fixed 23-byte record whose wire size is documented.</summary>
        public sealed class FhmFixed23Record
        {
            public FhmOpaqueBytes Data { get; set; } = new(new byte[23]);

            internal static FhmFixed23Record Read(FhmBinaryReader reader) => new() { Data = reader.ReadOpaqueBytes(23) };

            internal void WriteTo(FhmBinaryWriter writer) => FhmPlayerSerialization.WriteOpaqueFixed(writer, Data, 23, "fixed-23 record");
        }

        /// <summary>An opaque fixed 24-byte record whose wire size is documented.</summary>
        public sealed class FhmFixed24Record
        {
            public FhmOpaqueBytes Data { get; set; } = new(new byte[24]);

            internal static FhmFixed24Record Read(FhmBinaryReader reader) => new() { Data = reader.ReadOpaqueBytes(24) };

            internal void WriteTo(FhmBinaryWriter writer) => FhmPlayerSerialization.WriteOpaqueFixed(writer, Data, 24, "fixed-24 record");
        }

        /// <summary>A structurally known byte/signed-value pair.</summary>
        public sealed class FhmByteInt32Pair
        {
            public byte UnknownU1 { get; set; }
            public int UnknownS4 { get; set; }

            internal static FhmByteInt32Pair Read(FhmBinaryReader reader) =>
                new() { UnknownU1 = reader.ReadByte(), UnknownS4 = reader.ReadInt32() };

            internal void WriteTo(FhmBinaryWriter writer)
            {
                writer.WriteByte(UnknownU1);
                writer.WriteInt32(UnknownS4);
            }
        }

        internal static class FhmPlayerSerialization
        {
            internal static void ReadFixed(FhmBinaryReader reader, IList<byte> destination)
            {
                for (var index = 0; index < destination.Count; index++)
                {
                    destination[index] = reader.ReadByte();
                }
            }

            internal static void ReadFixed(FhmBinaryReader reader, IList<ushort> destination)
            {
                for (var index = 0; index < destination.Count; index++)
                {
                    destination[index] = reader.ReadUInt16();
                }
            }

            internal static void ReadFixed(FhmBinaryReader reader, IList<int> destination)
            {
                for (var index = 0; index < destination.Count; index++)
                {
                    destination[index] = reader.ReadInt32();
                }
            }

            internal static void ReadFixed(FhmBinaryReader reader, IList<double> destination)
            {
                for (var index = 0; index < destination.Count; index++)
                {
                    destination[index] = reader.ReadDouble();
                }
            }

            internal static void WriteFixed(FhmBinaryWriter writer, IList<byte> values, int expectedCount, string fieldName)
            {
                ValidateFixedCount(values, expectedCount, fieldName);
                foreach (var value in values)
                {
                    writer.WriteByte(value);
                }
            }

            internal static void WriteFixed(FhmBinaryWriter writer, IList<ushort> values, int expectedCount, string fieldName)
            {
                ValidateFixedCount(values, expectedCount, fieldName);
                foreach (var value in values)
                {
                    writer.WriteUInt16(value);
                }
            }

            internal static void WriteFixed(FhmBinaryWriter writer, IList<int> values, int expectedCount, string fieldName)
            {
                ValidateFixedCount(values, expectedCount, fieldName);
                foreach (var value in values)
                {
                    writer.WriteInt32(value);
                }
            }

            internal static void WriteFixed(FhmBinaryWriter writer, IList<double> values, int expectedCount, string fieldName)
            {
                ValidateFixedCount(values, expectedCount, fieldName);
                foreach (var value in values)
                {
                    writer.WriteDouble(value);
                }
            }

            internal static void ReadInt32List(FhmBinaryReader reader, IList<int> destination, string collectionName)
            {
                ReadList(reader, destination, collectionName, static source => source.ReadInt32());
            }

            internal static void ReadUInt16List(FhmBinaryReader reader, IList<ushort> destination, string collectionName)
            {
                ReadList(reader, destination, collectionName, static source => source.ReadUInt16());
            }

            internal static void ReadByteList(FhmBinaryReader reader, IList<byte> destination, string collectionName)
            {
                ReadList(reader, destination, collectionName, static source => source.ReadByte());
            }

            internal static void WriteInt32List(FhmBinaryWriter writer, IList<int> values, string collectionName)
            {
                WriteList(writer, values, collectionName, static (target, value) => target.WriteInt32(value));
            }

            internal static void WriteUInt16List(FhmBinaryWriter writer, IList<ushort> values, string collectionName)
            {
                WriteList(writer, values, collectionName, static (target, value) => target.WriteUInt16(value));
            }

            internal static void WriteByteList(FhmBinaryWriter writer, IList<byte> values, string collectionName)
            {
                WriteList(writer, values, collectionName, static (target, value) => target.WriteByte(value));
            }

            internal static void ReadList<T>(FhmBinaryReader reader, IList<T> destination, string collectionName, Func<FhmBinaryReader, T> readValue)
            {
                destination.Clear();
                var count = reader.ReadCount(collectionName);
                for (var index = 0; index < count; index++)
                {
                    destination.Add(readValue(reader));
                }
            }

            internal static void WriteList<T>(FhmBinaryWriter writer, IList<T> values, string collectionName, Action<FhmBinaryWriter, T> writeValue)
            {
                writer.WriteCount(values.Count, collectionName);
                foreach (var value in values)
                {
                    writeValue(writer, value);
                }
            }

            internal static void WriteOpaqueFixed(FhmBinaryWriter writer, FhmOpaqueBytes value, int expectedLength, string fieldName)
            {
                ArgumentNullException.ThrowIfNull(value);
                if (value.Value.Length != expectedLength)
                {
                    throw new FhmFormatException($"{fieldName} must contain exactly {expectedLength} bytes.");
                }

                writer.WriteOpaqueBytes(value);
            }

            private static void ValidateFixedCount<T>(IList<T> values, int expectedCount, string fieldName)
            {
                if (values.Count != expectedCount)
                {
                    throw new FhmFormatException($"{fieldName} must contain exactly {expectedCount} values.");
                }
            }
        }
