namespace dot_net_core_rest_api.Data;

/// <summary>
/// Coordinates multiple repository operations as a single atomic unit.
/// The service layer uses this to commit all tracked changes or wrap multiple
/// operations in an explicit database transaction that rolls back on failure.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes tracked by the current DbContext.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes <paramref name="operation"/> inside a database transaction.
    /// Automatically commits on success and rolls back on any exception.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default);
}
