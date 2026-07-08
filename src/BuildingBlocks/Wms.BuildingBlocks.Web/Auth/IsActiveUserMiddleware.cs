using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Web.Auth;

// Menolak request dari user yang sudah dinonaktifkan.
public sealed class IsActiveUserMiddleware(RequestDelegate next)
{
    public const string DisabledErrorCode = "auth.user_disabled";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var checker = context.RequestServices.GetService<IActiveUserChecker>();
            var userId = context.User.FindFirstValue(WmsClaimTypes.Subject);

            if (checker is not null && !string.IsNullOrEmpty(userId)
                && !await checker.IsActiveAsync(userId, context.RequestAborted))
            {
                await WriteDisabledAsync(context);
                return;
            }
        }

        await next(context);
    }

    private static async Task WriteDisabledAsync(HttpContext context)
    {
        var problem = ProblemDetailsMapper.ToProblemDetails(
            ResultErrorType.Forbidden,
            new Error(DisabledErrorCode, "Akun dinonaktifkan."),
            CorrelationId.Get(context));

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", context.RequestAborted);
    }
}
