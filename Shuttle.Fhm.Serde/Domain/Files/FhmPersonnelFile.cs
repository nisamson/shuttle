using System.Buffers.Binary;
using Shuttle.Fhm.Serde.Domain.Binary;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>Job assigned to a personnel record.</summary>
public enum FhmPersonnelJob : byte
{
    GmHeadCoach = 0,
    GeneralManager = 1,
    HeadCoach = 2,
    Owner = 3,
    Scout = 4,
    AssistantCoach = 5,
    Trainer = 7,
}

/// <summary>Offensive coaching preference.</summary>
public enum FhmOffensivePreference : ushort
{
    VeryDefensive = 0,
    Defensive = 1,
    Neutral = 2,
    Offensive = 3,
    VeryOffensive = 4,
}

/// <summary>Physical coaching preference.</summary>
public enum FhmPhysicalPreference : ushort
{
    VeryPhysical = 0,
    Physical = 1,
    Neutral = 2,
    NonPhysical = 3,
    VeryNonPhysical = 4,
}

/// <summary>How aggressively a coach matches lines against the opposition.</summary>
public enum FhmLineMatchingTendency : ushort
{
    VeryPassive = 0,
    Passive = 1,
    Balanced = 2,
    Aggressive = 3,
    VeryAggressive = 4,
}

/// <summary>How aggressively a coach handles goalies.</summary>
public enum FhmGoalieHandlingTendency : ushort
{
    VeryConservative = 0,
    Conservative = 1,
    Balanced = 2,
    Aggressive = 3,
    VeryAggressive = 4,
}

/// <summary>Whether a staff member favors prospects or veterans.</summary>
public enum FhmVeteranPreference : ushort
{
    LovesProspects = 0,
    PrefersProspects = 1,
    Balanced = 2,
    PrefersVeterans = 3,
    LovesVeterans = 4,
}

/// <summary>How willing a staff member is to innovate.</summary>
public enum FhmInnovationTendency : ushort
{
    VeryConservative = 0,
    Conservative = 1,
    Balanced = 2,
    Innovative = 3,
    VeryInnovative = 4,
}

/// <summary>How loyal or ruthless a staff member is.</summary>
public enum FhmLoyaltyTendency : ushort
{
    VeryRuthless = 0,
    Ruthless = 1,
    Balanced = 2,
    Loyal = 3,
    VeryLoyal = 4,
}

/// <summary>A lossless <c>personal.dat</c> model with editable verified fields.</summary>
public sealed class FhmPersonnelFile : IFhmSaveFile
{
    private const int HeaderLength = 8;
    private const int MinimumRecordLength = 0x135;
    private readonly byte[] sourceContent;
    private readonly HashSet<int> sourcePersonnelIds;

    private FhmPersonnelFile(byte[] sourceContent, IList<FhmPersonnelRecord> records)
    {
        this.sourceContent = sourceContent;
        sourcePersonnelIds = records.Select(record => record.PersonnelId).ToHashSet();
        Records = records;
    }

    public string RelativePath => "personal.dat";
    public int Version => ReadInt32(sourceContent, 0);
    public int NextPersonnelId => ReadInt32(sourceContent, 4);
    public IList<FhmPersonnelRecord> Records { get; }

    public static FhmPersonnelFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var content = buffer.ToArray();
        if (content.Length < HeaderLength)
        {
            throw new FhmFormatException("personal.dat is shorter than its 8-byte header.");
        }

        var nextPersonnelId = ReadInt32(content, 4);
        if (nextPersonnelId < 0 || nextPersonnelId > FhmPlayerWireMapper.MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid personal.dat next personnel ID {nextPersonnelId}.");
        }

        var startsById = new Dictionary<int, int>();
        for (var offset = HeaderLength; offset <= content.Length - MinimumRecordLength; offset++)
        {
            if (!IsRecordCandidate(content, offset, nextPersonnelId))
            {
                continue;
            }

            var personnelId = ReadInt32(content, offset + FhmPersonnelRecord.PersonnelIdOffset);
            startsById.TryAdd(personnelId, offset);
        }

        if (startsById.Count == 0 && nextPersonnelId != 0)
        {
            throw new FhmFormatException("personal.dat does not expose any personnel records.");
        }

        var starts = startsById.Values.Order().ToArray();
        var records = starts
            .Select((start, index) => CreateRecord(
                content,
                start,
                (index + 1 < starts.Length ? starts[index + 1] : content.Length) - start))
            .OrderBy(record => record.PersonnelId)
            .ToList();
        return new FhmPersonnelFile(content, records);
    }

    private static FhmPersonnelRecord CreateRecord(byte[] content, int start, int length)
    {
        try
        {
            return new FhmPersonnelRecord(content, start, length);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new FhmFormatException(
                $"Personnel {ReadInt32(content, start + FhmPersonnelRecord.PersonnelIdOffset)} at offset 0x{start:X} contains an unsupported documented value: {exception.Message}");
        }
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!Records.Select(record => record.PersonnelId).ToHashSet().SetEquals(sourcePersonnelIds))
        {
            throw new FhmFormatException("personal.dat personnel identities cannot be inserted, deleted, or replaced.");
        }

        var output = sourceContent.ToArray();
        foreach (var record in Records)
        {
            record.WriteKnownFields(output);
        }

        stream.Write(output);
    }

    private static bool IsRecordCandidate(byte[] content, int offset, int nextPersonnelId)
    {
        var birthYear = ReadInt32(content, offset + FhmPersonnelRecord.BirthDateOffset);
        var personnelId = ReadInt32(content, offset + FhmPersonnelRecord.PersonnelIdOffset);
        var teamRecordIndex = ReadInt32(content, offset + FhmPersonnelRecord.TeamRecordIndexOffset);
        var job = content[offset + FhmPersonnelRecord.JobOffset];
        var retired = content[offset + FhmPersonnelRecord.RetiredOffset];
        return ReadInt32(content, offset + 0x20) == -1
            && birthYear is >= 1800 and <= 2200
            && personnelId >= 0
            && personnelId < nextPersonnelId
            && teamRecordIndex >= -1
            && Enum.IsDefined((FhmPersonnelJob)job)
            && retired is 0 or 1 or byte.MaxValue;
    }

    internal static int ReadInt32(byte[] content, int offset) =>
        BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(offset, sizeof(int)));

    internal static ushort ReadUInt16(byte[] content, int offset) =>
        BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(offset, sizeof(ushort)));

    internal static void WriteInt32(byte[] content, int offset, int value) =>
        BinaryPrimitives.WriteInt32BigEndian(content.AsSpan(offset, sizeof(int)), value);

    internal static void WriteUInt16(byte[] content, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(content.AsSpan(offset, sizeof(ushort)), value);
}

/// <summary>One editable personnel record retained inside its original opaque framing.</summary>
public sealed class FhmPersonnelRecord
{
    private const int ReferenceRecordLength = 0x1B6;
    internal const int BirthDateOffset = 0x0C;
    internal const int TeamRecordIndexOffset = 0x28;
    internal const int PersonnelIdOffset = 0x30;
    internal const int JobOffset = 0x42;
    internal const int RetiredOffset = 0xE5;

    private readonly byte[] sourceContent;
    private readonly int sourceOffset;
    private readonly int sourceLength;
    private int? teamRecordIndex;
    private int? negotiating;
    private int? playerManagement;
    private int? coachingDefense;
    private int? coachingForwards;
    private int? coachingGoalies;
    private int? coachingProspects;
    private int? evaluateAbilities;
    private int? evaluatePotential;
    private int? defensiveSkills;
    private int? offensiveSkills;
    private int? physicalTraining;
    private int? tactics;
    private int? discipline;
    private int? selfPreservation;
    private int? motivation;
    private int? ingameTactics;
    private int? trainerSkill;
    private readonly byte retiredRaw;
    private bool retired;

    internal FhmPersonnelRecord(byte[] sourceContent, int sourceOffset, int sourceLength)
    {
        this.sourceContent = sourceContent;
        this.sourceOffset = sourceOffset;
        this.sourceLength = sourceLength;
        FirstNameNameId = ReadInt32(0);
        SurnameNameId = ReadInt32(4);
        NicknameNameId = ToNullableReference(ReadInt32(8));
        BirthDate = new(ReadInt32(0x0C), ReadInt32(0x10), ReadInt32(0x14));
        NationalityId = ReadUInt16(0x18);
        BirthCityId = ReadInt32(0x1C);
        PersonnelId = ReadInt32(PersonnelIdOffset);
        teamRecordIndex = ToNullableReference(ReadInt32(TeamRecordIndexOffset));
        Job = (FhmPersonnelJob)sourceContent[sourceOffset + JobOffset];
        Negotiating = ReadNullableRating(0x4C);
        OffensivePreference = (FhmOffensivePreference)ReadUInt16(0x4E);
        PlayerManagement = ReadNullableRating(0x50);
        PhysicalPreference = (FhmPhysicalPreference)ReadUInt16(0x52);
        CoachingDefense = ReadNullableRating(0x54);
        CoachingForwards = ReadNullableRating(0x56);
        CoachingGoalies = ReadNullableRating(0x58);
        CoachingProspects = ReadNullableRating(0x5A);
        EvaluateAbilities = ReadNullableRating(0x5C);
        EvaluatePotential = ReadNullableRating(0x5E);
        Reputation = ReadUInt16(0x6A);
        Salary = ReadInt32(0x6E);
        ContractLength = sourceContent[sourceOffset + 0x99] is 0 ? null : sourceContent[sourceOffset + 0x99];
        retiredRaw = sourceContent[sourceOffset + RetiredOffset];
        retired = retiredRaw == 1;
        OffensiveSkills = ReadNullableRating(TailOffset(0xF5));
        DefensiveSkills = ReadNullableRating(TailOffset(0xF7));
        BasedInLocationId = ReadUInt16(0xF9);
        PhysicalTraining = ReadPhysicalTraining();
        Tactics = ReadNullableRating(TailOffset(0x123));
        Discipline = ReadNullableRating(TailOffset(0x12D));
        SelfPreservation = ReadNullableRating(TailOffset(0x12F));
        Motivation = ReadNullableRating(TailOffset(0x133));
        IngameTactics = ReadNullableRating(TailOffset(0x135));
        TrainerSkill = ReadNullableRating(TailOffset(0x137));
        LineMatchingTendency = (FhmLineMatchingTendency)ReadUInt16(TailOffset(0x17E));
        GoalieHandlingTendency = (FhmGoalieHandlingTendency)ReadUInt16(TailOffset(0x180));
        VeteranPreference = (FhmVeteranPreference)ReadUInt16(TailOffset(0x182));
        InnovationTendency = (FhmInnovationTendency)ReadUInt16(TailOffset(0x184));
        LoyaltyTendency = (FhmLoyaltyTendency)ReadUInt16(TailOffset(0x186));
    }

    public int PersonnelId { get; }
    public int FirstNameNameId { get; set; }
    public int SurnameNameId { get; set; }
    public int? NicknameNameId { get; set; }
    public FhmDate BirthDate { get; set; }
    public ushort NationalityId { get; set; }
    public int BirthCityId { get; set; }
    public int? TeamRecordIndex
    {
        get => teamRecordIndex;
        set => teamRecordIndex = value is null or >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Team record index cannot be negative.");
    }
    public FhmPersonnelJob Job { get; set; }
    public int? Negotiating { get => negotiating; set => negotiating = ValidateRating(value, nameof(Negotiating)); }
    public FhmOffensivePreference OffensivePreference { get; set; }
    public int? PlayerManagement { get => playerManagement; set => playerManagement = ValidateRating(value, nameof(PlayerManagement)); }
    public FhmPhysicalPreference PhysicalPreference { get; set; }
    public int? CoachingDefense { get => coachingDefense; set => coachingDefense = ValidateRating(value, nameof(CoachingDefense)); }
    public int? CoachingForwards { get => coachingForwards; set => coachingForwards = ValidateRating(value, nameof(CoachingForwards)); }
    public int? CoachingGoalies { get => coachingGoalies; set => coachingGoalies = ValidateRating(value, nameof(CoachingGoalies)); }
    public int? CoachingProspects { get => coachingProspects; set => coachingProspects = ValidateRating(value, nameof(CoachingProspects)); }
    public int? EvaluateAbilities { get => evaluateAbilities; set => evaluateAbilities = ValidateRating(value, nameof(EvaluateAbilities)); }
    public int? EvaluatePotential { get => evaluatePotential; set => evaluatePotential = ValidateRating(value, nameof(EvaluatePotential)); }
    public ushort Reputation { get; set; }
    public int Salary { get; set; }
    public byte? ContractLength { get; set; }
    public bool Retired
    {
        get => retired;
        set => retired = value;
    }
    public int? DefensiveSkills { get => defensiveSkills; set => defensiveSkills = ValidateRating(value, nameof(DefensiveSkills)); }
    public int? OffensiveSkills { get => offensiveSkills; set => offensiveSkills = ValidateRating(value, nameof(OffensiveSkills)); }
    public ushort BasedInLocationId { get; set; }
    public int? PhysicalTraining { get => physicalTraining; set => physicalTraining = ValidateRating(value, nameof(PhysicalTraining)); }
    public int? Tactics { get => tactics; set => tactics = ValidateRating(value, nameof(Tactics)); }
    public int? Discipline { get => discipline; set => discipline = ValidateRating(value, nameof(Discipline)); }
    public int? SelfPreservation { get => selfPreservation; set => selfPreservation = ValidateRating(value, nameof(SelfPreservation)); }
    public int? Motivation { get => motivation; set => motivation = ValidateRating(value, nameof(Motivation)); }
    public int? IngameTactics { get => ingameTactics; set => ingameTactics = ValidateRating(value, nameof(IngameTactics)); }
    public int? TrainerSkill { get => trainerSkill; set => trainerSkill = ValidateRating(value, nameof(TrainerSkill)); }
    public FhmLineMatchingTendency LineMatchingTendency { get; set; }
    public FhmGoalieHandlingTendency GoalieHandlingTendency { get; set; }
    public FhmVeteranPreference VeteranPreference { get; set; }
    public FhmInnovationTendency InnovationTendency { get; set; }
    public FhmLoyaltyTendency LoyaltyTendency { get; set; }

    public byte[] GetSourceBytes() => sourceContent.AsSpan(sourceOffset, sourceLength).ToArray();

    internal void WriteKnownFields(byte[] output)
    {
        WriteInt32(output, 0, FirstNameNameId);
        WriteInt32(output, 4, SurnameNameId);
        WriteInt32(output, 8, NicknameNameId ?? -1);
        WriteInt32(output, 0x0C, BirthDate.Year);
        WriteInt32(output, 0x10, BirthDate.Month);
        WriteInt32(output, 0x14, BirthDate.Day);
        WriteUInt16(output, 0x18, NationalityId);
        WriteInt32(output, 0x1C, BirthCityId);
        WriteInt32(output, TeamRecordIndexOffset, TeamRecordIndex ?? -1);
        output[sourceOffset + JobOffset] = (byte)Job;
        WriteNullableRating(output, 0x4C, Negotiating);
        WriteUInt16(output, 0x4E, (ushort)OffensivePreference);
        WriteNullableRating(output, 0x50, PlayerManagement);
        WriteUInt16(output, 0x52, (ushort)PhysicalPreference);
        WriteNullableRating(output, 0x54, CoachingDefense);
        WriteNullableRating(output, 0x56, CoachingForwards);
        WriteNullableRating(output, 0x58, CoachingGoalies);
        WriteNullableRating(output, 0x5A, CoachingProspects);
        WriteNullableRating(output, 0x5C, EvaluateAbilities);
        WriteNullableRating(output, 0x5E, EvaluatePotential);
        WriteUInt16(output, 0x6A, Reputation);
        WriteInt32(output, 0x6E, Salary);
        output[sourceOffset + 0x99] = ContractLength ?? 0;
        if (Retired != (retiredRaw == 1))
        {
            output[sourceOffset + RetiredOffset] = Retired ? (byte)1 : (byte)0;
        }
        WriteNullableRating(output, TailOffset(0xF5), OffensiveSkills);
        WriteNullableRating(output, TailOffset(0xF7), DefensiveSkills);
        WriteUInt16(output, 0xF9, BasedInLocationId);
        WritePhysicalTraining(output);
        WriteNullableRating(output, TailOffset(0x123), Tactics);
        WriteNullableRating(output, TailOffset(0x12D), Discipline);
        WriteNullableRating(output, TailOffset(0x12F), SelfPreservation);
        WriteNullableRating(output, TailOffset(0x133), Motivation);
        WriteNullableRating(output, TailOffset(0x135), IngameTactics);
        WriteNullableRating(output, TailOffset(0x137), TrainerSkill);
        WriteUInt16(output, TailOffset(0x17E), (ushort)LineMatchingTendency);
        WriteUInt16(output, TailOffset(0x180), (ushort)GoalieHandlingTendency);
        WriteUInt16(output, TailOffset(0x182), (ushort)VeteranPreference);
        WriteUInt16(output, TailOffset(0x184), (ushort)InnovationTendency);
        WriteUInt16(output, TailOffset(0x186), (ushort)LoyaltyTendency);
    }

    private int ReadInt32(int relativeOffset) => FhmPersonnelFile.ReadInt32(sourceContent, sourceOffset + relativeOffset);
    private ushort ReadUInt16(int relativeOffset) => FhmPersonnelFile.ReadUInt16(sourceContent, sourceOffset + relativeOffset);
    private int? ReadNullableRating(int relativeOffset)
    {
        var value = ReadUInt16(relativeOffset);
        return value <= 20 ? value : null;
    }
    private int? ReadNullableByteRating(int relativeOffset)
    {
        var value = sourceContent[sourceOffset + relativeOffset];
        return value <= 20 ? value : null;
    }
    private int? ReadPhysicalTraining()
    {
        var offset = TailOffset(0xFA);
        return offset > 0xFA ? ReadNullableByteRating(offset) : null;
    }
    private void WriteInt32(byte[] output, int relativeOffset, int value) =>
        FhmPersonnelFile.WriteInt32(output, sourceOffset + relativeOffset, value);
    private void WriteUInt16(byte[] output, int relativeOffset, ushort value) =>
        FhmPersonnelFile.WriteUInt16(output, sourceOffset + relativeOffset, value);
    private void WriteNullableRating(byte[] output, int relativeOffset, int? value)
    {
        var sourceValue = ReadUInt16(relativeOffset);
        if (value is null && sourceValue is > 20 and < ushort.MaxValue)
        {
            return;
        }

        WriteUInt16(output, relativeOffset, ToWireRating(value));
    }
    private void WriteNullableByteRating(byte[] output, int relativeOffset, int? value)
    {
        if (value is not null)
        {
            output[sourceOffset + relativeOffset] = checked((byte)value.Value);
        }
    }
    private void WritePhysicalTraining(byte[] output)
    {
        var offset = TailOffset(0xFA);
        if (offset > 0xFA)
        {
            WriteNullableByteRating(output, offset, PhysicalTraining);
        }
    }
    private int TailOffset(int referenceOffset) => sourceLength - (ReferenceRecordLength - referenceOffset);

    private static int? ToNullableReference(int value) => value == -1 ? null : value;
    private static ushort ToWireRating(int? value) => value is null ? ushort.MaxValue : checked((ushort)value.Value);
    private static int? ValidateRating(int? value, string parameterName) =>
        value is null or >= 0 and <= 20
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "Personnel ratings must be between 0 and 20.");
}
