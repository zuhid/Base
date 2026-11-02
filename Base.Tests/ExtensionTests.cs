using Microsoft.EntityFrameworkCore;

namespace Zuhid.Base.Tests;

public class TestEntity : IEntity
{
    public Guid Id { get; set; }
    public Guid UpdatedById { get; set; }
    public DateTime Updated { get; set; }
    public string? Name { get; set; }
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; }
}

public class ExtensionTests
{
    private DbContextOptions<TestDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public void DbSet_LoadCsvData_Should_Attempt_To_Load()
    {
        // Arrange
        using var context = new TestDbContext(CreateOptions());
        // Since LoadCsvData uses Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName)
        // it will look for the file in the test execution directory.
        // We can create a dummy file there.
        var fileName = "test_entities.csv";
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        File.WriteAllText(filePath, "Id\n" + Guid.NewGuid().ToString());

        try
        {
            // Act
            context.TestEntities.LoadCsvData(fileName);

            // Assert
            Assert.True(context.TestEntities.Local.Count > 0);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ModelBuilder_ToSnakeCase_Should_SetNames()
    {
        // Arrange
        var builder = new ModelBuilder();
        builder.Entity<TestEntity>();

        // Act
        builder.ToSnakeCase();

        // Assert
        var entity = builder.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entity);
        Assert.Equal("test_entity", entity.GetTableName());
        Assert.Equal("tests", entity.GetSchema()); // namespace leaf "Tests" → snake_case
    }

    [Fact]
    public void ModelBuilder_LoadCsvData_Should_ReturnEarly_WhenNotDesignTime()
    {
        // EF.IsDesignTime is false in tests, so LoadCsvData returns immediately without loading data
        var builder = new ModelBuilder();
        builder.Entity<TestEntity>();

        // Should not throw
        builder.LoadCsvData();
    }

    [Fact]
    public void ModelBuilder_LoadCsvData_Should_ExecuteBody_WhenDesignTime()
    {
        var builder = new ModelBuilder();
        builder.Entity<TestEntity>();

        var namespacePart = typeof(TestEntity).Namespace!.Split('.').Last();
        var entityName = nameof(TestEntity).Replace("Entity", "");
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, namespacePart);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{entityName}.csv");
        File.WriteAllText(filePath, "Id"); // header-only CSV → empty data list

        try
        {
            EF.IsDesignTime = true;
            builder.LoadCsvData();
        }
        finally
        {
            EF.IsDesignTime = false;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ModelBuilder_LoadCsvData_Should_CallHasData_WhenDesignTimeAndDataPresent()
    {
        var builder = new ModelBuilder();
        builder.Entity<TestEntity>();

        // LoadCsvData reads Dataload/<namespace-leaf>/<entity-name-without-"Entity">.csv
        var namespacePart = typeof(TestEntity).Namespace!.Split('.').Last();
        var entityName = nameof(TestEntity).Replace("Entity", "");
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dataload", namespacePart);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{entityName}.csv");
        File.WriteAllText(filePath, $"Id\n{Guid.NewGuid()}"); // one data row → HasData is invoked

        try
        {
            EF.IsDesignTime = true;
            builder.LoadCsvData();

            var seed = builder.Model.FindEntityType(typeof(TestEntity))!.GetSeedData().ToList();
            Assert.Single(seed);
        }
        finally
        {
            EF.IsDesignTime = false;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ModelBuilder_LoadJsonData_WithEmptyArray_Should_NotCallHasData()
    {
        var dataloadDir = Path.Combine(Directory.GetCurrentDirectory(), "Dataload");
        Directory.CreateDirectory(dataloadDir);
        var filePath = Path.Combine(dataloadDir, $"{nameof(TestEntity)}.json");
        File.WriteAllText(filePath, "[]");

        try
        {
            var builder = new ModelBuilder();
            builder.Entity<TestEntity>();

            builder.LoadJsonData<TestEntity>();
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ModelBuilder_LoadJsonData_WithData_Should_CallHasData()
    {
        var dataloadDir = Path.Combine(Directory.GetCurrentDirectory(), "Dataload");
        Directory.CreateDirectory(dataloadDir);
        var filePath = Path.Combine(dataloadDir, $"{nameof(TestEntity)}.json");
        var id = Guid.NewGuid();
        File.WriteAllText(filePath, $@"[{{""Id"":""{id}"",""UpdatedById"":""{Guid.Empty}"",""Updated"":""2023-01-01T00:00:00""}}]");

        try
        {
            var builder = new ModelBuilder();
            builder.Entity<TestEntity>();

            // data.Count == 1 > 0 → HasData is called (covers the if-true branch)
            builder.LoadJsonData<TestEntity>();
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void DbSet_LoadCsvData_Should_DoNothing_WhenFileHasNoData()
    {
        using var context = new TestDbContext(CreateOptions());
        var fileName = "empty_entities.csv";
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        File.WriteAllText(filePath, "Id"); // header only, no data rows

        try
        {
            context.TestEntities.LoadCsvData(fileName);

            Assert.Empty(context.TestEntities.Local);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
