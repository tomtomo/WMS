using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Persistence.Configurations;

internal sealed class OperatorActivityConfiguration : IEntityTypeConfiguration<OperatorActivity>
{
    public void Configure(EntityTypeBuilder<OperatorActivity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("operator_activity");
        builder.HasKey(activity => new { activity.OperatorId, activity.Period });
    }
}
