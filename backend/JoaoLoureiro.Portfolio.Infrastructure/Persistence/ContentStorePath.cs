using Microsoft.Data.Sqlite;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

internal static class ContentStorePath
{
    public static string CreateConnectionString(string configuredPath)
    {
        var fullPath = System.IO.Path.GetFullPath(configuredPath);
        var directory = System.IO.Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The content store path must include a directory.");
        }

        Directory.CreateDirectory(directory);

        return new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }
}
