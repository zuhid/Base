using Zuhid.Base.Query;

namespace Zuhid.Base.Tests;

// QueryRequest.MaxTake is static; this collection serializes the tests that mutate it.
[Collection("MaxTake")]
public class QueryRequestTests
{
    [Fact]
    public void Take_Should_ClampToMaxTake()
    {
        var original = QueryRequest.MaxTake;
        try
        {
            QueryRequest.MaxTake = 60;
            var request = new QueryRequest();

            Assert.Equal(50, request.Take);

            request.Take = 30;
            Assert.Equal(30, request.Take);

            request.Take = 500;
            Assert.Equal(60, request.Take);
        }
        finally
        {
            QueryRequest.MaxTake = original;
        }
    }
}
