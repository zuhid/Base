using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Zuhid.Base.Query;

namespace Zuhid.Base.Tests;

internal sealed class PassValidator : IValidator<TestModel>
{
    public bool Validate(TestModel model) => true;
}

internal sealed class FailValidator : IValidator<TestModel>
{
    public bool Validate(TestModel model) => false;
}

// Concrete controller exposing the protected *Core members so they can be exercised directly.
internal sealed class TestApiController(
    TestRepository repository,
    MapperBase<TestEntity, TestModel> mapper,
    IValidator<TestModel> validator)
    : ControllerApiBase<TestDbContext, TestRepository, TestEntity, TestModel, MapperBase<TestEntity, TestModel>, IValidator<TestModel>>(repository, mapper, validator)
{
    public Task<List<TestModel>> GetAll() => GetCore();
    public Task<List<TestModel>> GetById(Guid id) => GetCore(id);
    public Task<List<TestModel>> GetByRequest(QueryRequest request) => GetCore(request);
    public Task GetStreamByRequest(QueryRequest request) => GetStreamCore(request);
    public Task<int> CountByRequest(QueryRequest request) => CountCore(request);
    public Task<UpdatedResponse?> Add(TestModel model) => AddCore(model);
    public Task<UpdatedResponse?> Update(TestModel model) => UpdateCore(model);
    public Task Delete(Guid id) => DeleteCore(id);
}

public class ControllerApiBaseTests
{
    private static TestApiController CreateController(SqliteTestContext harness, IValidator<TestModel> validator) =>
        new(new TestRepository(harness.Context), new MapperBase<TestEntity, TestModel>(), validator);

    [Fact]
    public async Task GetCore_Should_ReturnAllRows()
    {
        using var harness = new SqliteTestContext();
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid() });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid() });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var controller = CreateController(harness, new PassValidator());

        var result = await controller.GetAll();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetCore_Should_ReturnRowById()
    {
        using var harness = new SqliteTestContext();
        var id = Guid.NewGuid();
        harness.Context.TestEntities.Add(new TestEntity { Id = id });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var controller = CreateController(harness, new PassValidator());

        var result = await controller.GetById(id);

        Assert.Single(result);
        Assert.Equal(id, result[0].Id);
    }

    [Fact]
    public async Task GetCore_Should_ReturnFilteredAndSortedRows_When_QueryRequestProvided()
    {
        using var harness = new SqliteTestContext();
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Beta" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Alpha" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Gamma" });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var controller = CreateController(harness, new PassValidator());

        var result = await controller.GetByRequest(new QueryRequest
        {
            Filters = [new FilterRule { FilterBy = "Name", FilterValue = "a", FilterOperator = FieldOperator.Contains }],
            Sorts = [new SortRule { SortBy = "Name" }]
        });

        Assert.Equal(["Alpha", "Beta", "Gamma"], result.Select(m => m.Name));
    }

    [Fact]
    public async Task GetStreamCore_Should_WriteAllMatchingRowsAsNdjson()
    {
        using var harness = new SqliteTestContext();
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Beta" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Alpha" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Gamma" });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var controller = CreateController(harness, new PassValidator());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } }
        };

        await controller.GetStreamByRequest(new QueryRequest
        {
            Sorts = [new SortRule { SortBy = "Name" }]
        });

        Assert.Equal("application/x-ndjson", controller.Response.ContentType);
        controller.Response.Body.Position = 0;
        using var reader = new StreamReader(controller.Response.Body);
        var lines = (await reader.ReadToEndAsync()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = lines.SelectMany(line => JsonSerializer.Deserialize<TestModel[]>(line, JsonSerializerOptions.Web)!);
        Assert.Equal(["Alpha", "Beta", "Gamma"], result.Select(m => m.Name));
    }

    [Fact]
    public async Task CountCore_Should_ReturnFilteredRows_When_QueryRequestProvided()
    {
        using var harness = new SqliteTestContext();
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Beta" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Alpha" });
        harness.Context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Gamma" });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();
        var controller = CreateController(harness, new PassValidator());

        var count = await controller.CountByRequest(new QueryRequest
        {
            Filters = [new FilterRule { FilterBy = "Name", FilterValue = "a", FilterOperator = FieldOperator.Contains }]
        });

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AddCore_Should_ReturnNull_When_ValidationFails()
    {
        using var harness = new SqliteTestContext();
        var controller = CreateController(harness, new FailValidator());

        var response = await controller.Add(new TestModel { Id = Guid.NewGuid() });

        Assert.Null(response);
    }

    [Fact]
    public async Task AddCore_Should_StampUpdatedAndPersist_When_Valid()
    {
        using var harness = new SqliteTestContext();
        var controller = CreateController(harness, new PassValidator());
        var before = DateTime.UtcNow.AddSeconds(-1);
        var model = new TestModel { Id = Guid.NewGuid() };

        var response = await controller.Add(model);

        Assert.NotNull(response);
        Assert.True(response.Updated >= before, "AddCore should stamp Updated with the current UTC time.");
        harness.Context.ChangeTracker.Clear();
        Assert.Single(await controller.GetById(model.Id));
    }

    [Fact]
    public async Task AddCore_Should_StampUpdatedById_FromUserClaims()
    {
        using var harness = new SqliteTestContext();
        var controller = CreateController(harness, new PassValidator());
        var userId = Guid.NewGuid();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                // Inbound claim mapping folds sub (the username) and nameid into the same claim
            // type — the non-guid username must be skipped in favor of the user id.
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ]))
            }
        };
        var model = new TestModel { Id = Guid.NewGuid(), UpdatedById = Guid.NewGuid() }; // client-supplied value must be ignored

        await controller.Add(model);

        harness.Context.ChangeTracker.Clear();
        var saved = await controller.GetById(model.Id);
        Assert.Equal(userId, saved[0].UpdatedById);
    }

    [Fact]
    public async Task AddCore_Should_StampEmptyUpdatedById_When_NoUser()
    {
        using var harness = new SqliteTestContext();
        var controller = CreateController(harness, new PassValidator());
        var model = new TestModel { Id = Guid.NewGuid(), UpdatedById = Guid.NewGuid() };

        await controller.Add(model);

        harness.Context.ChangeTracker.Clear();
        var saved = await controller.GetById(model.Id);
        Assert.Equal(Guid.Empty, saved[0].UpdatedById);
    }

    [Fact]
    public async Task UpdateCore_Should_ReturnNull_When_ValidationFails()
    {
        using var harness = new SqliteTestContext();
        var controller = CreateController(harness, new FailValidator());

        var response = await controller.Update(new TestModel { Id = Guid.NewGuid() });

        Assert.Null(response);
    }

    [Fact]
    public async Task UpdateCore_Should_StampUpdatedAndPersist_When_Valid()
    {
        using var harness = new SqliteTestContext();
        var controller = CreateController(harness, new PassValidator());
        var id = Guid.NewGuid();
        await controller.Add(new TestModel { Id = id });
        harness.Context.ChangeTracker.Clear();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var response = await controller.Update(new TestModel { Id = id });

        Assert.NotNull(response);
        Assert.True(response.Updated >= before, "UpdateCore should stamp Updated with the current UTC time.");
    }

    [Fact]
    public async Task DeleteCore_Should_RemoveRow()
    {
        using var harness = new SqliteTestContext();
        var controller = CreateController(harness, new PassValidator());
        var id = Guid.NewGuid();
        await controller.Add(new TestModel { Id = id });
        harness.Context.ChangeTracker.Clear();

        await controller.Delete(id);

        Assert.Empty(await controller.GetById(id));
    }
}
