using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.Login;

public sealed record LoginCommand(string Username, string Password) : ICommand<LoginResponse>;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
