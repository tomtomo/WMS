using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Shared.Security;

// Gunakan format PHC agar hash lama tetap dapat diverifikasi saat parameter Argon2id berubah.
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Argon2id = nama varian algoritma di RFC 9106, bukan pelanggaran casing.")]
public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    private readonly int _memoryKibibytes;
    private readonly int _iterations;
    private readonly int _parallelism;

    public Argon2idPasswordHasher()
        : this(memoryKibibytes: 19456, iterations: 2, parallelism: 1)
    {
    }

    // Dibuka untuk test agar bisa membuat hash dengan parameter lama.
    internal Argon2idPasswordHasher(int memoryKibibytes, int iterations, int parallelism)
    {
        _memoryKibibytes = memoryKibibytes;
        _iterations = iterations;
        _parallelism = parallelism;
    }

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Compute(password, salt, _memoryKibibytes, _iterations, _parallelism);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"$argon2id$v=19$m={_memoryKibibytes},t={_iterations},p={_parallelism}${ToBase64NoPadding(salt)}${ToBase64NoPadding(hash)}");
    }

    public bool Verify(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentException.ThrowIfNullOrEmpty(hash);

        if (!TryParse(hash, out var parsed))
        {
            return false;
        }

        // Hitung ulang memakai parameter dari hash yang tersimpan.
        var computed = Compute(password, parsed.Salt, parsed.MemoryKibibytes, parsed.Iterations, parsed.Parallelism);
        return CryptographicOperations.FixedTimeEquals(computed, parsed.Hash);
    }

    // Perlu rehash jika hash valid
    public bool NeedsRehash(string hash)
    {
        ArgumentException.ThrowIfNullOrEmpty(hash);

        if (!TryParse(hash, out var parsed))
        {
            return true;
        }

        return parsed.MemoryKibibytes != _memoryKibibytes
            || parsed.Iterations != _iterations
            || parsed.Parallelism != _parallelism;
    }

    private static byte[] Compute(string password, byte[] salt, int memoryKibibytes, int iterations, int parallelism)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKibibytes,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        return argon2.GetBytes(HashSizeBytes);
    }

    private static bool TryParse(string encodedHash, out ParsedHash parsed)
    {
        parsed = default!;

        var parts = encodedHash.Split('$');
        if (parts is not ["", "argon2id", "v=19", var parameters, var saltPart, var hashPart])
        {
            return false;
        }

        var parameterPairs = parameters.Split(',');
        if (parameterPairs.Length != 3
            || !TryParseParameter(parameterPairs[0], "m", out var memory)
            || !TryParseParameter(parameterPairs[1], "t", out var iterations)
            || !TryParseParameter(parameterPairs[2], "p", out var parallelism)
            || !TryFromBase64NoPadding(saltPart, out var salt)
            || !TryFromBase64NoPadding(hashPart, out var hash))
        {
            return false;
        }

        parsed = new ParsedHash(memory, iterations, parallelism, salt, hash);
        return true;
    }

    private static bool TryParseParameter(string pair, string expectedKey, out int value)
    {
        value = 0;
        var keyValue = pair.Split('=');
        return keyValue.Length == 2
            && string.Equals(keyValue[0], expectedKey, StringComparison.Ordinal)
            && int.TryParse(keyValue[1], NumberStyles.None, CultureInfo.InvariantCulture, out value)
            && value > 0;
    }

    private static string ToBase64NoPadding(byte[] value) => Convert.ToBase64String(value).TrimEnd('=');

    private static bool TryFromBase64NoPadding(string value, out byte[] bytes)
    {
        var padded = value.PadRight(value.Length + ((4 - (value.Length % 4)) % 4), '=');
        try
        {
            bytes = Convert.FromBase64String(padded);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private sealed record ParsedHash(int MemoryKibibytes, int Iterations, int Parallelism, byte[] Salt, byte[] Hash);
}
