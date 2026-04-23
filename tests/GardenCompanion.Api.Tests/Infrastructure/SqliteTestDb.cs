namespace GardenCompanion.Api.Tests.Infrastructure;

public sealed class SqliteTestDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    private SqliteTestDb(SqliteConnection connection)
    {
        _connection = connection;
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;
    }

    public static async Task<SqliteTestDb> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var testDb = new SqliteTestDb(connection);
        await using var context = testDb.CreateContext();
        await context.Database.MigrateAsync();

        return testDb;
    }

    public AppDbContext CreateContext() => new(_options);

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
