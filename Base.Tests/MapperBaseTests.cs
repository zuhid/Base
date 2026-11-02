namespace Zuhid.Base.Tests;

public class MapperBaseTests
{
    private sealed class SourceModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class TargetEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Unmapped { get; set; } = "default"; // no matching source property
        public string ReadOnly => "computed"; // CanWrite == false → skipped by the mapper
    }

    [Fact]
    public void GetEntity_Should_CopyMatchingProperties()
    {
        var mapper = new MapperBase<TargetEntity, SourceModel>();
        var model = new SourceModel { Id = Guid.NewGuid(), Name = "Ada" };

        var entity = mapper.GetEntity(model);

        Assert.Equal(model.Id, entity.Id);
        Assert.Equal("Ada", entity.Name);
    }

    [Fact]
    public void GetEntity_Should_LeaveUnmatchedTargetPropertiesAtDefault()
    {
        var mapper = new MapperBase<TargetEntity, SourceModel>();

        var entity = mapper.GetEntity(new SourceModel { Name = "x" });

        Assert.Equal("default", entity.Unmapped);
    }

    [Fact]
    public void GetEntityList_Should_MapEveryModel()
    {
        var mapper = new MapperBase<TargetEntity, SourceModel>();
        var models = new List<SourceModel>
        {
            new() { Id = Guid.NewGuid(), Name = "one" },
            new() { Id = Guid.NewGuid(), Name = "two" }
        };

        var entities = mapper.GetEntityList(models);

        Assert.Equal(2, entities.Count);
        Assert.Equal("one", entities[0].Name);
        Assert.Equal("two", entities[1].Name);
    }
}
