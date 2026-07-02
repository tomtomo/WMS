namespace Wms.BuildingBlocks.Domain.Auditing;

// Audit standar - diisi EF interceptor dari ICurrentUser, concurrency lewat xmin.
public interface IAuditable
{
    string CreatedBy { get; set; }

    DateTimeOffset CreatedAt { get; set; }

    string? ModifiedBy { get; set; }

    DateTimeOffset? ModifiedAt { get; set; }
}
