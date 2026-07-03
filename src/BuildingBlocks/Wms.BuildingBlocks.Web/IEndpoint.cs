using Microsoft.AspNetCore.Routing;

namespace Wms.BuildingBlocks.Web;

public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder app);
}
