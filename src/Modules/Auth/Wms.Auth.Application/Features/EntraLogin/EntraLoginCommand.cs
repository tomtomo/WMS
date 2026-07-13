using Wms.Auth.Application.Features.Login;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.EntraLogin;

// Tukarkan token Entra yang sudah tervalidasi menjadi JWT internal seperti pada login lokal.
public sealed record EntraLoginCommand(string IdToken) : ICommand<LoginResponse>;
