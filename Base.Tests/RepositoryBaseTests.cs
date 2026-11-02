using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zuhid.Base.Query;

namespace Zuhid.Base.Tests;

public class TestModel : IModel
{
    public Guid Id { get; set; }
    public Guid UpdatedById { get; set; }
    public DateTime Updated { get; set; }
    public string? Name { get; set; }
}

public class TestRepository(TestDbContext context)
    : RepositoryBase<TestDbContext, TestEntity, TestModel>(context)
{
    protected override IQueryable<TestModel> Query =>
        Context.Set<TestEntity>().Select(e => new TestModel
        {
            Id = e.Id,
            UpdatedById = e.UpdatedById,
            Updated = e.Updated,
            Name = e.Name
        });

    public override async Task<List<OptionModel>> GetOptions() =>
        await Context.Set<TestEntity>().Select(e => new OptionModel { Id = e.Id, Text = e.Name ?? "" }).ToListAsync();
}

// RepositoryBase.Delete (and ControllerApiBase.DeleteCore) call ExecuteDeleteAsync, which the EF Core
// InMemory provider does not support. Sqlite (already referenced by the test project) runs real SQL, so
// these tests use an in-memory Sqlite database kept alive by holding the connection open.
public sealed class SqliteTestContext : IDisposable
{
    private readonly SqliteConnection _connection;
    public TestDbContext Context { get; }

    public SqliteTestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new TestDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

public class RepositoryBaseTests
{
    [Fact]
    public async Task Get_Should_ReturnAllRows_When_NoIdProvided()
    {
        using var harness = new SqliteTestContext();
        for (var i = 0; i < 60; i++)
        {
            harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid() });
        }
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var repository = new TestRepository(harness.Context);

        var result = await repository.Get();

        Assert.Equal(60, result.Count);
    }

    [Fact]
    public async Task Get_Should_ReturnMatchingRow_When_IdProvided()
    {
        using var harness = new SqliteTestContext();
        var id = Guid.NewGuid();
        harness.Context.TestEntities.Add(new TestEntity { Id = id });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid() });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var repository = new TestRepository(harness.Context);

        var result = await repository.Get(id);

        Assert.Single(result);
        Assert.Equal(id, result[0].Id);
    }

    [Fact]
    public async Task Get_Should_FilterSortAndPage_When_QueryRequestProvided()
    {
        using var harness = new SqliteTestContext();
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Apple" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Apricot" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Banana" });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var repository = new TestRepository(harness.Context);

        var result = await repository.Get(new QueryRequest
        {
            Filters = [new FilterRule { FilterBy = "Name", FilterValue = "Ap", FilterOperator = FieldOperator.Contains }],
            Sorts = [new SortRule { SortBy = "Name", Descending = true }],
            Skip = 0,
            Take = 10
        });

        Assert.Equal(["Apricot", "Apple"], result.Select(m => m.Name));
    }

    [Fact]
    public async Task Get_Should_ReturnPagedRows_When_QueryRequestHasNoFilterOrSort()
    {
        using var harness = new SqliteTestContext();
        for (var i = 0; i < 3; i++)
        {
            harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid() });
        }
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var repository = new TestRepository(harness.Context);

        var result = await repository.Get(new QueryRequest { Skip = 1, Take = 1 });

        Assert.Single(result);
    }

    [Fact]
    public async Task Count_Should_ReturnTotalRows()
    {
        using var harness = new SqliteTestContext();
        for (var i = 0; i < 60; i++)
        {
            harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid() });
        }
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var repository = new TestRepository(harness.Context);

        var count = await repository.Count();

        Assert.Equal(60, count);
    }

    [Fact]
    public async Task Count_Should_ReturnFilteredRows_When_QueryRequestProvided()
    {
        using var harness = new SqliteTestContext();
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Apple" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Apricot" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Banana" });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var repository = new TestRepository(harness.Context);

        var count = await repository.Count(new QueryRequest
        {
            Filters = [new FilterRule { FilterBy = "Name", FilterValue = "Ap", FilterOperator = FieldOperator.Contains }]
        });

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Add_Should_PersistEntity_AndReturnUpdatedTimestamp()
    {
        using var harness = new SqliteTestContext();
        var repository = new TestRepository(harness.Context);
        var updated = DateTime.UtcNow;
        var entity = new TestEntity { Id = Guid.NewGuid(), Updated = updated };

        var response = await repository.Add(entity);

        Assert.Equal(updated, response.Updated);
        harness.Context.ChangeTracker.Clear();
        Assert.Single(await repository.Get(entity.Id));
    }

    [Fact]
    public async Task Update_Should_PersistChanges_AndReturnUpdatedTimestamp()
    {
        using var harness = new SqliteTestContext();
        var repository = new TestRepository(harness.Context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Updated = DateTime.UtcNow.AddDays(-1) };
        await repository.Add(entity);
        harness.Context.ChangeTracker.Clear();

        var newTimestamp = DateTime.UtcNow;
        entity.Updated = newTimestamp;
        var response = await repository.Update(entity);

        Assert.Equal(newTimestamp, response.Updated);
    }

    [Fact]
    public async Task Delete_Should_RemoveEntity()
    {
        using var harness = new SqliteTestContext();
        var repository = new TestRepository(harness.Context);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        await repository.Add(entity);
        harness.Context.ChangeTracker.Clear();

        await repository.Delete(entity.Id);

        Assert.Empty(await repository.Get(entity.Id));
    }

    [Fact]
    public async Task Add_Should_AttachEntityToException_When_SaveFails()
    {
        var harness = new SqliteTestContext();
        var repository = new TestRepository(harness.Context);
        harness.Dispose(); // disposing the context forces the Add to throw, exercising the catch block
        var entity = new TestEntity { Id = Guid.NewGuid(), Updated = DateTime.UtcNow };

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => repository.Add(entity));

        Assert.Same(entity, exception.Data["entity"]);
    }

    [Fact]
    public async Task Update_Should_AttachEntityToException_When_SaveFails()
    {
        var harness = new SqliteTestContext();
        var repository = new TestRepository(harness.Context);
        harness.Dispose();
        var entity = new TestEntity { Id = Guid.NewGuid(), Updated = DateTime.UtcNow };

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => repository.Update(entity));

        Assert.Same(entity, exception.Data["entity"]);
    }
}

// GetStream batches by the static QueryRequest.MaxTake; this collection serializes the tests that mutate it.
[Collection("MaxTake")]
public class RepositoryBaseStreamTests
{
    private static async Task<List<TestModel[]>> ToBatchList(IAsyncEnumerable<TestModel[]> stream)
    {
        var batches = new List<TestModel[]>();
        await foreach (var batch in stream)
        {
            batches.Add(batch);
        }
        return batches;
    }

    private static async Task<List<TestModel>> ToList(IAsyncEnumerable<TestModel[]> stream) =>
        [.. (await ToBatchList(stream)).SelectMany(batch => batch)];

    [Fact]
    public async Task GetStream_Should_ReturnAllRows_AcrossMultipleBatches()
    {
        var original = QueryRequest.MaxTake;
        try
        {
            QueryRequest.MaxTake = 2;
            using var harness = new SqliteTestContext();
            for (var i = 0; i < 5; i++)
            {
                harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = $"Row{i}" });
            }
            await harness.Context.SaveChangesAsync();
            harness.Context.ChangeTracker.Clear();
            var repository = new TestRepository(harness.Context);

            var batches = await ToBatchList(repository.GetStream(new QueryRequest()));

            Assert.Equal([2, 2, 1], batches.Select(b => b.Length));
            var result = batches.SelectMany(b => b).ToList();
            Assert.Equal(5, result.Select(m => m.Id).Distinct().Count());
        }
        finally
        {
            QueryRequest.MaxTake = original;
        }
    }

    [Fact]
    public async Task GetStream_Should_FilterAndSort_ButIgnoreSkipAndTake()
    {
        var original = QueryRequest.MaxTake;
        try
        {
            QueryRequest.MaxTake = 2;
            using var harness = new SqliteTestContext();
            harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Apple" });
            harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Apricot" });
            harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Avocado" });
            harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Banana" });
            await harness.Context.SaveChangesAsync();
            harness.Context.ChangeTracker.Clear();
            var repository = new TestRepository(harness.Context);

            var result = await ToList(repository.GetStream(new QueryRequest
            {
                Filters = [new FilterRule { FilterBy = "Name", FilterValue = "Ap", FilterOperator = FieldOperator.StartsWith }],
                Sorts = [new SortRule { SortBy = "Name", Descending = true }],
                Skip = 1, // paging inputs must not reduce the stream
                Take = 1
            }));

            Assert.Equal(["Apricot", "Apple"], result.Select(m => m.Name));
        }
        finally
        {
            QueryRequest.MaxTake = original;
        }
    }
}
