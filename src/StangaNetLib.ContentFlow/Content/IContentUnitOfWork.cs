namespace StangaNetLib.ContentFlow.Content;

/// <summary>
/// Optional commit boundary after a batch of content mutations (e.g. a scheduler tick).
/// Default is a no-op; replace with a Scoped implementation when using a unit-of-work store (EF Core, etc.).
/// </summary>
public interface IContentUnitOfWork
{
    /// <summary>Persists pending changes accumulated during the current scope.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
