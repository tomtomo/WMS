using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<RefreshAccessTokenResponse>;

public sealed record RefreshAccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
