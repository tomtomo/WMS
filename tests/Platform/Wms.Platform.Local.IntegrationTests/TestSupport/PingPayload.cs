namespace Wms.Platform.Local.IntegrationTests.TestSupport;

// Payload delayed-task test; terserialisasi ke storage Hangfire.
public sealed record PingPayload(string Message);
