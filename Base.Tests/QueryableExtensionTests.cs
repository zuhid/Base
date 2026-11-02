using Zuhid.Base.Query;

namespace Zuhid.Base.Tests;

public class QueryableExtensionTests
{
    private sealed class Row
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int? Rank { get; set; }
        public long Size { get; set; }
        public double Price { get; set; }
        public decimal Cost { get; set; }
        public bool Active { get; set; }
        public DateOnly Day { get; set; }
        public DateTime Created { get; set; }
    }

    private static readonly Guid KnownId = Guid.NewGuid();

    private static IQueryable<Row> Rows() => new List<Row>
    {
        new() { Id = KnownId, Name = "Alpha", Rank = 1, Size = 10, Price = 1.5, Cost = 10.5m, Active = true, Day = new DateOnly(2026, 1, 1), Created = new DateTime(2026, 1, 1) },
        new() { Id = Guid.NewGuid(), Name = "Bravo", Rank = 2, Size = 20, Price = 2.5, Cost = 20.5m, Active = false, Day = new DateOnly(2026, 2, 1), Created = new DateTime(2026, 2, 1) },
        new() { Id = Guid.NewGuid(), Name = "Charlie", Rank = null, Size = 30, Price = 3.5, Cost = 30.5m, Active = false, Day = new DateOnly(2026, 3, 1), Created = new DateTime(2026, 3, 1) }
    }.AsQueryable();

    [Fact]
    public void Filter_Should_MatchGuidProperty_When_OperatorIsEqual()
    {
        var result = Rows().Filter("Id", KnownId.ToString()).ToList();

        Assert.Equal(["Alpha"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchExactString_When_OperatorIsEqual()
    {
        var result = Rows().Filter("Name", "Bravo").ToList();

        Assert.Equal(["Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchStringIgnoringCase_When_OperatorIsEqual()
    {
        var result = Rows().Filter("Name", "bRaVo").ToList();

        Assert.Equal(["Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_ExcludeStringIgnoringCase_When_OperatorIsNotEqual()
    {
        var result = Rows().Filter("Name", "ALPHA", FieldOperator.NotEqual).ToList();

        Assert.Equal(["Bravo", "Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchSubstringIgnoringCase_When_OperatorIsContains()
    {
        var result = Rows().Filter("Name", "RAV", FieldOperator.Contains).ToList();

        Assert.Equal(["Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchPrefixIgnoringCase_When_OperatorIsStartsWith()
    {
        var result = Rows().Filter("Name", "aL", FieldOperator.StartsWith).ToList();

        Assert.Equal(["Alpha"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchSuffixIgnoringCase_When_OperatorIsEndsWith()
    {
        var result = Rows().Filter("Name", "LIE", FieldOperator.EndsWith).ToList();

        Assert.Equal(["Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_ExcludeMatches_When_OperatorIsNotEqual()
    {
        var result = Rows().Filter("Active", "true", FieldOperator.NotEqual).ToList();

        Assert.Equal(["Bravo", "Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_CompareLessThan_On_NullableIntegerProperty()
    {
        var result = Rows().Filter("Rank", "2", FieldOperator.LessThan).ToList();

        Assert.Equal(["Alpha"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_CompareLessThanOrEqual_On_LongProperty()
    {
        var result = Rows().Filter("Size", "20", FieldOperator.LessThanOrEqual).ToList();

        Assert.Equal(["Alpha", "Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_CompareGreaterThan_On_DoubleProperty()
    {
        var result = Rows().Filter("Price", "2.5", FieldOperator.GreaterThan).ToList();

        Assert.Equal(["Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_CompareGreaterThanOrEqual_On_DecimalProperty()
    {
        var result = Rows().Filter("Cost", "20.5", FieldOperator.GreaterThanOrEqual).ToList();

        Assert.Equal(["Bravo", "Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchRange_When_OperatorIsBetween()
    {
        var result = Rows().Filter("Day", "2026-01-15,2026-02-15", FieldOperator.Between).ToList();

        Assert.Equal(["Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchSingleBound_When_BetweenHasOneValue()
    {
        var result = Rows().Filter("Created", "2026-02-01T00:00:00", FieldOperator.Between).ToList();

        Assert.Equal(["Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchNullValues_When_OperatorIsNull()
    {
        var result = Rows().Filter("Rank", null, FieldOperator.Null).ToList();

        Assert.Equal(["Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_ExcludeNullValues_When_OperatorIsNotNull()
    {
        var result = Rows().Filter("Rank", null, FieldOperator.NotNull).ToList();

        Assert.Equal(["Alpha", "Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchSubstring_When_OperatorIsContains()
    {
        var result = Rows().Filter("name", "rav", FieldOperator.Contains).ToList();

        Assert.Equal(["Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchPrefix_When_OperatorIsStartsWith()
    {
        var result = Rows().Filter("Name", "Al", FieldOperator.StartsWith).ToList();

        Assert.Equal(["Alpha"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_MatchSuffix_When_OperatorIsEndsWith()
    {
        var result = Rows().Filter("Name", "lie", FieldOperator.EndsWith).ToList();

        Assert.Equal(["Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_Throw_When_StringOperatorUsedOnNonStringProperty()
    {
        Assert.Throws<ArgumentException>(() => Rows().Filter("Rank", "1", FieldOperator.Contains));
    }

    [Fact]
    public void Filter_Should_Throw_When_OperatorIsUnknown()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rows().Filter("Name", "x", (FieldOperator)999));
    }

    [Fact]
    public void Filter_Should_CombineRulesWithAnd_When_MultipleRulesProvided()
    {
        var result = Rows().Filter(
        [
            new FilterRule { FilterBy = "Active", FilterValue = "false" },
            new FilterRule { FilterBy = "Name", FilterValue = "o", FilterOperator = FieldOperator.EndsWith }
        ]).ToList();

        Assert.Equal(["Bravo"], result.Select(r => r.Name));
    }

    [Fact]
    public void Filter_Should_ReturnUnchangedQuery_When_NoRulesProvided()
    {
        var result = Rows().Filter([]).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_Should_Throw_When_PropertyDoesNotExist()
    {
        Assert.Throws<ArgumentException>(() => Rows().Filter("Missing", "x"));
    }

    [Fact]
    public void Sort_Should_OrderAscending_ByDefault()
    {
        var result = Rows().Sort("name").ToList();

        Assert.Equal(["Alpha", "Bravo", "Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Sort_Should_OrderDescending_When_Requested()
    {
        var result = Rows().Sort("Size", descending: true).ToList();

        Assert.Equal(["Charlie", "Bravo", "Alpha"], result.Select(r => r.Name));
    }

    [Fact]
    public void Sort_Should_ChainThenByDescending_When_MultipleRulesProvided()
    {
        var result = Rows().Sort([new SortRule { SortBy = "Active" }, new SortRule { SortBy = "Name", Descending = true }]).ToList();

        Assert.Equal(["Charlie", "Bravo", "Alpha"], result.Select(r => r.Name));
    }

    [Fact]
    public void Sort_Should_ChainThenBy_When_MultipleRulesProvided()
    {
        var result = Rows().Sort([new SortRule { SortBy = "Active", Descending = true }, new SortRule { SortBy = "Name" }]).ToList();

        Assert.Equal(["Alpha", "Bravo", "Charlie"], result.Select(r => r.Name));
    }

    [Fact]
    public void Sort_Should_ReturnUnchangedQuery_When_NoRulesProvided()
    {
        var result = Rows().Sort([]).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Sort_Should_Throw_When_PropertyDoesNotExist()
    {
        Assert.Throws<ArgumentException>(() => Rows().Sort("Missing"));
    }
}
