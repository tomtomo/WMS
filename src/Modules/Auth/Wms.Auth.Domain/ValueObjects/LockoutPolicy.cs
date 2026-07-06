using Wms.BuildingBlocks.Domain.Primitives;

namespace Wms.Auth.Domain.ValueObjects;

// Kebijakan lockout setelah beberapa kali gagal login.
public sealed class LockoutPolicy : ValueObject
{
    // Default. Dapat dipindahkan ke konfigurasi jika diperlukan.
    public static readonly LockoutPolicy Default = new(maxFailedAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

    private LockoutPolicy(int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        MaxFailedAttempts = maxFailedAttempts;
        LockoutDuration = lockoutDuration;
    }

    public int MaxFailedAttempts { get; }

    public TimeSpan LockoutDuration { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxFailedAttempts;
        yield return LockoutDuration;
    }
}
