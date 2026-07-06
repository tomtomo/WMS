using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
