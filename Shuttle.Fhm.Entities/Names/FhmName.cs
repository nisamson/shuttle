namespace Shuttle.Fhm.Entities.Names;

public record FhmName(int NameId, string Name, int GroupId, ushort CategoryWeight, bool FlagA, bool FlagB, bool FlagC);
