using LibGit2Sharp;

namespace Shuttle.GameArchive;

public sealed class GameArchiveGit(string username, string password) {
    private readonly UsernamePasswordCredentials credentials = new() {
        Username = username,
        Password = password,
    };

    public Repository Clone(string remote, string path, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        try {
            Repository.Clone(remote, path, new CloneOptions {
                RecurseSubmodules = false,
                FetchOptions = {
                    CredentialsProvider = (_, _, _) => credentials,
                    OnTransferProgress = _ => !cancellationToken.IsCancellationRequested,
                },
            });
            cancellationToken.ThrowIfCancellationRequested();
            return new Repository(path);
        } catch (LibGit2SharpException ex) {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException($"Git clone failed: {Redact(ex.Message)}");
        }
    }

    public bool CommitAndPush(
        Repository repository, IEnumerable<string> current, IEnumerable<string> removed,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var path in current) {
            cancellationToken.ThrowIfCancellationRequested();
            repository.Index.Add(path);
        }
        foreach (var path in removed) {
            cancellationToken.ThrowIfCancellationRequested();
            repository.Index.Remove(path);
        }
        repository.Index.Add(GameArchiveManifest.FileName);
        repository.Index.Write();
        if (repository.Head.Tip.Tree.Id == repository.Index.WriteToTree().Id)
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        var identity = new Signature("shuttle-bot", "shuttle-bot@users.noreply.github.com", DateTimeOffset.UtcNow);
        repository.Commit("Archive SHL game files", identity, identity);
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<string>();
        try {
            repository.Network.Push(repository.Head, new PushOptions {
                CredentialsProvider = (_, _, _) => credentials,
                OnPushStatusError = error => errors.Add(Redact(error.Message)),
                OnPushTransferProgress = (_, _, _) => !cancellationToken.IsCancellationRequested,
            });
            cancellationToken.ThrowIfCancellationRequested();
        } catch (LibGit2SharpException ex) {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException($"Git push failed: {Redact(ex.Message)}");
        }
        if (errors.Count > 0)
            throw new InvalidOperationException($"Git push rejected: {string.Join("; ", errors)}");
        return true;
    }

    private string Redact(string value) => value.Replace(credentials.Password, "[redacted]", StringComparison.Ordinal);
}
