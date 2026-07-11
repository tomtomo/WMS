namespace Wms.Platform.Azure.Persistence;

// Data projection yang diterima dari change feed, beserta selisih waktu sejak terakhir diupdate.
public sealed record ProjectionChange(string ProjectionType, string Key, DateTimeOffset UpdatedAt, TimeSpan Lag);
