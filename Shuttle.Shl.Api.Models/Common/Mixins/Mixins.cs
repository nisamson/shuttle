namespace Shuttle.Shl.Api.Models.Common.Mixins;

public interface IHasId {
    int Id { get; }
}

public interface INamed {
    string Name { get; }
}

public interface ILeagueSeason {
    int League { get; }
    int Season { get; }
}