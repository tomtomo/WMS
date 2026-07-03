using System.ComponentModel.DataAnnotations;

namespace Wms.BuildingBlocks.Application.UnitTests.TestDoubles;

// Options sample untuk test AddValidatedOptions.
public sealed class SampleOptions
{
    [Range(1, 100)]
    public int MaxItems { get; set; }
}
