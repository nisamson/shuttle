namespace SHLAnalytics.ML.CV.Trainer.Commands;

public interface ICommand {
    
    string Name { get; }
    
    Task Execute(CancellationToken token = default);
}
