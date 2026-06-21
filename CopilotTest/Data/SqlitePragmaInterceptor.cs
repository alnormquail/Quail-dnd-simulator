using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CopilotTest.Data;

/// <summary>
/// Applies per-connection SQLite PRAGMAs each time a (pooled) connection opens.
///
/// <para><c>busy_timeout</c> tells SQLite to wait up to N milliseconds for a
/// lock to clear instead of immediately throwing "database is locked". With
/// WAL mode (set once at startup) readers never block, so the only contention
/// left is two writers overlapping — and a short busy_timeout lets the second
/// one wait its turn rather than erroring. This matters once several party
/// members are hitting the app at the same time.</para>
///
/// Connection-level pragmas don't persist, so they must be re-applied on every
/// open — hence an interceptor rather than a one-off startup statement.
/// </summary>
public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    private const string Pragmas = "PRAGMA busy_timeout = 5000;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = Pragmas;
        cmd.ExecuteNonQuery();
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = Pragmas;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }
}
