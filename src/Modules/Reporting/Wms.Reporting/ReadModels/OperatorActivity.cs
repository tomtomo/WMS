namespace Wms.Reporting.ReadModels;

// Projection produktivitas operator per (operator, period-harian)
public sealed class OperatorActivity
{
    public Guid OperatorId { get; set; }

    public DateOnly Period { get; set; }

    public int PutawayCount { get; set; }

    public int PickCount { get; set; }
}
