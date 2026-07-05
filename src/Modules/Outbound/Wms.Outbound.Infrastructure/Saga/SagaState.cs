using System.Diagnostics.CodeAnalysis;

namespace Wms.Outbound.Infrastructure.Saga;

// State machine saga orchestrated compensation milik Outbound.
public sealed class SagaState
{
    public const string StartedStatus = "Started";

    private SagaState(string sagaId, string sagaType, string state, string status, DateTimeOffset now)
    {
        SagaId = sagaId;
        SagaType = sagaType;
        State = state;
        Status = status;
        CreatedAt = now;
        UpdatedAt = now;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola EF standar.")]
    private SagaState()
    {
        SagaId = string.Empty;
        SagaType = string.Empty;
        State = string.Empty;
        Status = string.Empty;
    }

    public string SagaId { get; private set; }

    public string SagaType { get; private set; }

    // Snapshot data saga (JSON).
    public string State { get; private set; }

    public string Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static SagaState Start(string sagaId, string sagaType, string state, DateTimeOffset now) =>
        new(sagaId, sagaType, state, StartedStatus, now);
}
