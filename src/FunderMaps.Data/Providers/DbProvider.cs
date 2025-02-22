﻿using Microsoft.Extensions.Options;
using System.Data.Common;
using System.Runtime.ExceptionServices;

namespace FunderMaps.Data.Providers;

/// <summary>
///     Database provider.
/// </summary>
internal abstract class DbProvider
{
    protected readonly DbProviderOptions _options;

    /// <summary>
    ///     Create new instance.
    /// </summary>
    public DbProvider(IOptions<DbProviderOptions> options)
        => _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    ///     Create a new connection instance.
    /// </summary>
    /// <returns><see cref="DbConnection"/> instance.</returns>
    public abstract DbConnection ConnectionScope();

    /// <summary>
    ///     Open database connection.
    /// </summary>
    /// <param name="token">The cancellation instruction.</param>
    /// <returns>See <see cref="DbConnection"/>.</returns>
    public virtual async Task<DbConnection> OpenConnectionScopeAsync(CancellationToken token = default)
    {
        var connection = ConnectionScope();
        await connection.OpenAsync(token);
        return connection;
    }

    /// <summary>
    ///     Create command on the database connection.
    /// </summary>
    /// <param name="cmdText">The text of the query.</param>
    /// <param name="connection">Database connection, see <see cref="DbConnection"/>.</param>
    /// <returns>See <see cref="DbCommand"/>.</returns>
    public virtual DbCommand CreateCommand(string cmdText, DbConnection connection)
    {
        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        var cmd = connection.CreateCommand();
        cmd.CommandText = cmdText;
        return cmd;
    }

    /// <summary>
    ///     Handle database exception.
    /// </summary>
    /// <param name="edi">Captured exception.</param>
    internal virtual void HandleException(ExceptionDispatchInfo edi) => edi.Throw();
}
