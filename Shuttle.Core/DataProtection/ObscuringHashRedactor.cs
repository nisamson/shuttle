using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Compliance.Redaction;

namespace Shuttle.ServiceDefaults.DataProtection;

/// <summary>
/// Used for obfuscation. This is not a secure hash and should only be used for obfuscation purposes.
/// The output is a hex string of the hash value.
/// </summary>
public class ObscuringHashRedactor : Redactor {

    public const int Md5HexLength = 32;
    
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination) {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, Md5HexLength, nameof(destination));
        var charArray = ArrayPool<char>.Shared.Rent(Md5HexLength);
        var bytesNeeded = Encoding.UTF8.GetByteCount(source);
        var bytes = ArrayPool<byte>.Shared.Rent(bytesNeeded);
        Span<byte> hashBytes = stackalloc byte[16];
        try {
            source.CopyTo(charArray);
            Encoding.UTF8.GetBytes(charArray, 0, source.Length, bytes, 0);
            MD5.HashData(bytes, hashBytes);
            return Convert.TryToHexString(hashBytes, destination, out var charsWritten) ? charsWritten : 0;
        } finally {
            ArrayPool<char>.Shared.Return(charArray, clearArray: true);
            ArrayPool<byte>.Shared.Return(bytes, clearArray: true);
        }
    }
    public override int GetRedactedLength(ReadOnlySpan<char> input) {
        return Md5HexLength;
    }
}
