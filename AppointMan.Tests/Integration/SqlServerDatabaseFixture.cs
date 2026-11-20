using AppointMan.Domain.Entities;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AppointMan.Tests.Integration;

/// <summary>
/// Gerçek SQL Server LocalDB üzerinde geçici bir şema oluşturur.
/// Uygulamanın migration/seed akışına dokunmaz; yalnızca test izolasyonu sağlar.
/// </summary>
public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"AppointMan_Test_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        // TestData.NewBusiness adresi 34 plaka kodunu kullanır.
        context.Add(new City(34, "Istanbul", "TR"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition("SqlServerIntegration")]
public sealed class SqlServerIntegrationCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
}
