namespace Wms.BuildingBlocks.Application.UnitTests.TestDoubles;

// Perekam urutan langkah pipeline.
public sealed class PipelineRecorder
{
    private readonly object _gate = new();
    private readonly List<string> _steps = [];

    public IReadOnlyList<string> Steps
    {
        get
        {
            lock (_gate)
            {
                return _steps.ToList();
            }
        }
    }

    public void Add(string step)
    {
        lock (_gate)
        {
            _steps.Add(step);
        }
    }
}
