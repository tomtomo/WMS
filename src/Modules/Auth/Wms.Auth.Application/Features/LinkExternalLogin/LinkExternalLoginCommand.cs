using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.LinkExternalLogin;

// Tautkan akun eksternal ke pengguna WMS secara manual tanpa membuat pengguna baru otomatis.
[RequiresPermission(AuthPermissions.ManageUser)]
public sealed record LinkExternalLoginCommand(Guid UserId, string Provider, string Subject) : ICommand<Guid>;
