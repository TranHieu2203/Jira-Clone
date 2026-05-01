using System.Buffers.Binary;
using System.Security.Cryptography;

namespace BB.Common;

public interface IGuidGenerator
{
    Guid NewId();
}

/// <summary>
/// UUID v7 generator (time-ordered, RFC 9562). Giảm fragment index khi dùng làm PK.
/// </summary>
public sealed class UuidV7Generator : IGuidGenerator
{
    public Guid NewId() => CreateUuidV7(DateTimeOffset.UtcNow);

    internal static Guid CreateUuidV7(DateTimeOffset timestamp)
    {
        Span<byte> bytes = stackalloc byte[16];
        long unixMs = timestamp.ToUnixTimeMilliseconds();

        // First 6 bytes: 48-bit unix_ts_ms (big-endian).
        BinaryPrimitives.WriteInt64BigEndian(bytes[..8], unixMs << 16);

        // 10 bytes random.
        Span<byte> rand = stackalloc byte[10];
        RandomNumberGenerator.Fill(rand);
        rand.CopyTo(bytes[6..]);

        // Set version (7) in byte 6 high nibble.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        // Set variant (RFC 4122 / 9562) in byte 8 high bits.
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        // Guid uses mixed endian for first 3 fields on little-endian platforms; rebuild bytes for ctor.
        return new Guid(new ReadOnlySpan<byte>(bytes.ToArray()), bigEndian: true);
    }
}
