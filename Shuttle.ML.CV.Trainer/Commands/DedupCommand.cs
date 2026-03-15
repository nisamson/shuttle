using Microsoft.Extensions.Logging;
using Shuttle.ML.CV.Trainer.Services.Dedup;

namespace Shuttle.ML.CV.Trainer.Commands;

public class DedupCommand : ICommand {

    private readonly ILogger<DedupCommand> logger;
    private readonly IDuplicateImageIdentifier duplicateImageIdentifier;
    
    public DedupCommand(IDuplicateImageIdentifier duplicateImageIdentifier, ILogger<DedupCommand> logger) {
        this.duplicateImageIdentifier = duplicateImageIdentifier;
        this.logger = logger;
    }

    public string Name => DedupOptions.DedupCommandName;
    public Task Execute(CancellationToken token = default) {
        throw new NotImplementedException();
    }
}
