namespace Wms.BuildingBlocks.Application.Abstractions;

// Marker permission
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequiresPermissionAttribute(string permission) : Attribute
{
    public string Permission { get; } = permission;
}
