namespace Shuttle.Fhm.Serde.Domain.Model;

/// <summary>FHM's broad player archetype. The serialized underlying value is a quint16.</summary>
public enum FhmPlayingRole : ushort
{
    Agitator,
    DefensiveDefenceman,
    CheckingForward,
    Enforcer,
    Goalscorer,
    Grinder,
    OffensiveDefenceman,
    OffensiveForward,
    Playmaker,
    PowerForward,
    Screener,
    TwoWayDefenceman,
    TwoWayForward,
    StandardGoalie,
    PuckhandlingGoalie,
    None = ushort.MaxValue,
}

/// <summary>FHM squad-status category.</summary>
public enum FhmSquadStatus : ushort
{
    None,
    FranchisePlayer,
    StarPlayer,
    Leader,
    BlueChipProspect,
    Prospect,
    FringeProspect,
    Policeman,
    Depth,
    StartingGoalie,
    BackupGoalie,
}

/// <summary>One of the twelve team-tactics zones.</summary>
public enum FhmTacticZone : ushort
{
    Breakout,
    NeutralZoneOffense,
    OffensiveZoneAttack,
    Forecheck,
    NeutralZoneCoverage,
    DefensiveZoneCoverage,
    PowerPlayBreakout,
    PowerPlayOffensiveZoneAttack,
    PowerPlayDefense,
    PenaltyKillForecheck,
    PenaltyKillDefensiveZoneCoverage,
    PenaltyKillAttack,
}

/// <summary>Team offensive orientation.</summary>
public enum FhmOffensiveOrientation : ushort
{
    Offensive,
    Balanced,
    Defensive,
}

/// <summary>Team physical orientation.</summary>
public enum FhmPhysicalOrientation : ushort
{
    Physical,
    Balanced,
    NonPhysical,
}

/// <summary>The thirteen ordered active-line groups serialized by teams and stored lines.</summary>
public enum FhmLineGroup
{
    EvenStrengthForwards,
    EvenStrengthDefence,
    PowerPlayFiveOnFour,
    PowerPlayFiveOnThree,
    PenaltyKillFourOnFive,
    PenaltyKillThreeOnFive,
    FourOnFour,
    ThreeOnThree,
    PowerPlayFourOnThree,
    PenaltyKillThreeOnFour,
    ExtraAttackers,
    ShootoutOrder,
    Goalies,
}

/// <summary>The eight tendency slots serialized by player roles and team tactics.</summary>
public enum FhmTendency
{
    Aggressiveness,
    Attacking,
    Backchecking,
    Hitting,
    Passing,
    Pressure,
    Shooting,
    Tempo,
}

/// <summary>The six position-rating slots at the even indices of a player position vector.</summary>
public enum FhmPlayerPosition
{
    Goalie,
    LeftDefenceman,
    RightDefenceman,
    LeftWing,
    Centre,
    RightWing,
}

/// <summary>Known aggregate-stat game types.</summary>
public enum FhmGameType : ushort
{
    RegularSeason = 1,
    Preseason = 4,
    Playoffs = 5,
}

/// <summary>Known fan-happiness events. Other values are preserved as raw values.</summary>
public enum FhmFanHappinessEvent : ushort
{
    StartingHappiness,
    WonGame,
    WonPlayoffRound,
    WonLeagueChampionship,
    WonCompetition,
    PickedPlayerInFirstRound,
    TicketPriceChange,
    SignedExtremelyTalentedPlayer,
    SignedVeryTalentedPlayer,
    SignedLivingLegend,
    SignedExtremelyPopularPlayer,
    SignedPopularPlayer,
    AcquiredExtremelyTalentedPlayer,
    AcquiredVeryTalentedPlayer,
    AcquiredLivingLegend,
    AcquiredExtremelyPopularPlayer,
    AcquiredPopularPlayer,
    FiredStaffMember,
    HiredLivingLegend,
    Promoted,
    NewSeasonOptimism,
    LostGame,
    LostPlayoffRound,
    MissedPlayoffs,
    IncreasedTicketPrices,
    ExtremelyTalentedPlayerBecameFreeAgent,
    VeryTalentedPlayerBecameFreeAgent,
    LivingLegendBecameFreeAgent,
    ExtremelyPopularPlayerBecameFreeAgent,
    PopularPlayerBecameFreeAgent,
    LostExtremelyTalentedPlayer,
    LostVeryTalentedPlayer,
    LostLivingLegend,
    LostExtremelyPopularPlayer,
    LostPopularPlayer,
    TradedLongtimePlayer,
    FiredLivingLegend,
    Relegated,
    BeatMainRival,
    BeatRival,
    LostToMainRival,
    LostToRival,
    DraftedFirstOverall,
    TradedRecentDraftPick,
    TradedFirstRoundPick,
    ReacquiredLongtimePlayer,
    ResignedLongtimePlayer,
    UpsetInPlayoffs,
    FiveGameLosingStreak,
    TenGameLosingStreak,
    FiveGameWinningStreak,
    TenGameWinningStreak,
    TicketPriceChangeAlternate,
    OffIceIncident,
    MidseasonPerformanceEvaluation,
    SeasonEndingPerformanceEvaluation,
    HiredHeadCoach,
    HiredGeneralManager,
}
