using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Web;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class HttpContextCorrelationContextTests
{
    [Fact]
    public void Reads_the_correlation_id_set_on_the_http_context()
    {
        var httpContext = new DefaultHttpContext();
        CorrelationId.Set(httpContext, "corr-web-7");
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var context = new HttpContextCorrelationContext(accessor);

        context.CorrelationId.Should().Be("corr-web-7");
    }

    [Fact]
    public void Returns_null_when_there_is_no_http_context()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };

        var context = new HttpContextCorrelationContext(accessor);

        context.CorrelationId.Should().BeNull();
    }
}
