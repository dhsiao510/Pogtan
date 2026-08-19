using System.Buffers.Binary;
using System.Text;

namespace Pogtan.Live;

/// <summary>
/// Dialect-B wire helpers (session port 47611), verified 2026-08-18.
///
/// Frame:   [u16be outerLen][u16be msgid][payload …]
///          outerLen = total − 4 = payload (bytes following the msgid).
/// Envelope (payload): [18 00][u16be innerLen][u8 tag][u24be subLen][content …]
///          innerLen = outerLen − 4 (bytes after the innerLen field)
///          subLen   = innerLen − 12 (bytes after the subLen field, minus a trailing u32+u32?)
///          tag      = 0x02 client→server, 0x00 server→client
/// Strings: [u16be charCount][UTF-16LE chars] (no terminator).
/// </summary>
public static class DialectB
{
    public const ushort Flag = 0x1800;
    public const byte TagRequest = 0x02;
    public const byte TagResponse = 0x00;

    /// <summary>Wrap a payload in a frame and write it to the stream.</summary>
    public static void WriteFrame(Stream stream, ushort msgId, byte[] payload)
    {
        Span<byte> head = stackalloc byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(head, (ushort)payload.Length); // outerLen = bytes after msgid
        BinaryPrimitives.WriteUInt16BigEndian(head[2..], msgId);
        stream.Write(head);
        stream.Write(payload);
    }

    /// <summary>Read one frame; returns (msgId, payload-after-msgid).</summary>
    public static async Task<(ushort MsgId, byte[] Payload)> ReadFrameAsync(Stream stream, CancellationToken ct = default)
    {
        byte[] head = new byte[4];
        await stream.ReadExactlyAsync(head, ct);
        ushort outerLen = BinaryPrimitives.ReadUInt16BigEndian(head);
        ushort msgId = BinaryPrimitives.ReadUInt16BigEndian(head.AsSpan(2));
        byte[] payload = new byte[outerLen];
        await stream.ReadExactlyAsync(payload, ct);
        return (msgId, payload);
    }
}

/// <summary>Big-endian packet builder with the dialect-B envelope patched in at <see cref="ToArray"/>.</summary>
public sealed class PacketWriter
{
    private readonly MemoryStream ms = new();
    private readonly byte tag;

    public PacketWriter(byte tag = DialectB.TagResponse)
    {
        this.tag = tag;
        U16(DialectB.Flag);
        U16(0);          // innerLen placeholder (offset 2)
        U8(tag);
        U24(0);          // subLen placeholder (offset 5)
    }

    public PacketWriter U8(byte v) { ms.WriteByte(v); return this; }
    public PacketWriter U16(ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b, v); ms.Write(b); return this; }
    public PacketWriter U24(uint v) { ms.WriteByte((byte)(v >> 16)); ms.WriteByte((byte)(v >> 8)); ms.WriteByte((byte)v); return this; }
    public PacketWriter U32(uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); ms.Write(b); return this; }
    public PacketWriter U32Le(uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); ms.Write(b); return this; }
    public PacketWriter Bytes(params byte[] v) { ms.Write(v); return this; }
    public PacketWriter Zeros(int n) { for (int i = 0; i < n; i++) ms.WriteByte(0); return this; }

    /// <summary>Counted UTF-16LE string: u16-LE char count + chars, no terminator.
    /// NOTE: count is LITTLE-endian even though framing lengths are big-endian (verified).</summary>
    public PacketWriter Utf16(string s)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(b, (ushort)s.Length);
        ms.Write(b);
        ms.Write(Encoding.Unicode.GetBytes(s));
        return this;
    }

    /// <summary>Finalize: patch innerLen/subLen and return the payload (frame header NOT included).</summary>
    public byte[] ToArray()
    {
        byte[] body = ms.ToArray();
        ushort innerLen = (ushort)(body.Length - 4);   // bytes after innerLen field
        uint subLen = (uint)(innerLen - 12);           // bytes after subLen field, minus 8 (verified constant across msgids)
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2), innerLen);
        body[5] = (byte)(subLen >> 16);
        body[6] = (byte)(subLen >> 8);
        body[7] = (byte)subLen;
        return body;
    }
}
