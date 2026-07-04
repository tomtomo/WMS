using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#9 — Sengaja kosong. Nomor tak dipakai ulang agar penomoran FF stabil.
public sealed class Ff09_IntentionallyEmpty
{
    [Fact(Skip = "FF#9 intentionally empty — number not reused")]
    public void Reserved_slot_not_reused()
    {
        // Tak ada assertion: slot nomor FF#9 sengaja  kosong.
    }
}
