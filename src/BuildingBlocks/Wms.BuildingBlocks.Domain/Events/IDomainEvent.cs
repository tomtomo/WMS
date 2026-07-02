using System.Diagnostics.CodeAnalysis;

namespace Wms.BuildingBlocks.Domain.Events;

// Marker domain event. Event konkret berupa record, yang diterjemahkan ke integration event di Application.
[SuppressMessage(
    "Major Code Smell",
    "S4023:Interfaces should not be empty",
    Justification = "Marker DDD yang disengaja: menandai event domain in-process tanpa kontrak member.")]
public interface IDomainEvent
{
}
