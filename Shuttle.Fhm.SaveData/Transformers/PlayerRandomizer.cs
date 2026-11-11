using Microsoft.Extensions.Logging;
using Shuttle.Fhm.SaveData.Transformers.Settings;
using Shuttle.Fhm.Serde.Sqlite;

namespace Shuttle.Fhm.SaveData.Transformers;

public class PlayerRandomizer : ISaveTransformer {
    private readonly ILogger<PlayerRandomizer> logger;
    private readonly PlayerRandomizerSettings settings;

    public PlayerRandomizer(ILogger<PlayerRandomizer> logger, PlayerRandomizerSettings settings) {
        this.logger = logger;
        this.settings = settings;
    }

    public Task TransformAsync(FhmSaveSqliteContext context, CancellationToken cancellationToken = default) {
        throw new NotImplementedException("Player randomization is not implemented.");
    }
}
