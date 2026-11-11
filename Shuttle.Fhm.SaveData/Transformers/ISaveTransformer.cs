using Shuttle.Fhm.Serde.Sqlite;

namespace Shuttle.Fhm.SaveData.Transformers;

public interface ISaveTransformer {
    Task TransformAsync(FhmSaveSqliteContext context, CancellationToken cancellationToken = default);
}
