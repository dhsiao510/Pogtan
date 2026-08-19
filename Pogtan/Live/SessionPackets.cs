namespace Pogtan.Live;

/// <summary>
/// Session-port (47611) message constructors. Layouts verified byte-for-byte against
/// captures of a full login (see bnb-notes docs/protocol-notes.md §3);
/// unknown fields carry the captured constant with a TODO.
/// Request/response msgids are adjacent: 0x0b→0x0c, 0x2d→0x2e, 0x0d→0x0e.
/// </summary>
public static class SessionPackets
{
    public const ushort MsgSessionAuthRequest = 0x000B;
    public const ushort MsgSessionAuth = 0x000C;
    public const ushort MsgPlayerInfoRequest = 0x002D;
    public const ushort MsgPlayerInfo = 0x002E;
    public const ushort MsgAckRequest = 0x000D;
    public const ushort MsgAck = 0x000E;

    /// <summary>
    /// 0x000C session auth reply — hands the client its nmco_session cookie ("NGP"+accountId).
    /// Captured layout: u32 0, u32 sessionId, u32 0, str "nmco_session", str cookie, 12 zero bytes.
    /// </summary>
    public static byte[] SessionAuth(string accountId, uint sessionId) =>
        new PacketWriter()
            .U32(0)
            .U32(sessionId)
            .U32(0)
            .Utf16("nmco_session")
            .Utf16("NGP" + accountId)
            .Zeros(12)
            .ToArray();

    /// <summary>
    /// 0x002E player info reply — carries the display name among fixed-size fields.
    /// Captured layout (139B for a 6-char name): prefix through u32le 0x12bd, str name,
    /// then flag/u32le 0x1388 tail. Field meanings TODO (0x12bd=4797 avatar? 0x1388=5000 lucci?).
    /// </summary>
    public static byte[] PlayerInfo(string displayName, uint sessionId) =>
        new PacketWriter()
            .U32(0)
            .U32(sessionId)
            .Zeros(19)
            .U8(0x01)
            .Zeros(16)
            .U32Le(0x12BD)          // TODO: meaning (captured 0x000012bd = 4797)
            .Utf16(displayName)
            .Zeros(8)
            .U8(0x01)
            .Zeros(15)
            .U8(0x01).U8(0x01)
            .Zeros(2)
            .U32Le(0x1388)          // TODO: meaning (captured 0x00001388 = 5000)
            .Zeros(33)
            .ToArray();

    /// <summary>0x000E ack — fully static in captures: u32 0, [00 04 00 01], u32 0.</summary>
    public static byte[] Ack() =>
        new PacketWriter()
            .U32(0)
            .Bytes(0x00, 0x04, 0x00, 0x01)
            .U32(0)
            .ToArray();
}
