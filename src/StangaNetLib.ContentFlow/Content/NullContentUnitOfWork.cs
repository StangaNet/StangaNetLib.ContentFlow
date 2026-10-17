namespace StangaNetLib.ContentFlow.Content;

/// <summary>
/// No-op <see cref="IContentUnitOfWork"/> used by the default in-memory stack.
/// </summary>
internal sealed class NullContentUnitOfWork : IContentUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
