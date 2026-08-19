using Pogtan.Live;
using Xunit;

namespace Pogtan.Tests;

/// <summary>
/// Synthetic-packet tests: every constructor must reproduce the reference server's
/// bytes captured 2026-08-18 (bnb-notes docs/protocol-notes.md §3). Hex fixtures are
/// the raw s2c frames from mitm-20260818-225347-full3-tcp47611-*.
/// </summary>
public class SessionPacketTests
{
    // Full captured frames (u16be len + u16be msgid + payload).
    private const string AuthCapture =
        "0054000c1800005000000044000000006c3d091c00000000" +
        "0c006e006d0063006f005f00730065007300730069006f006e00" + // "nmco_session"
        "0c004e0047005000640068007300690061006f00350031003000" + // "NGPdhsiao510"
        "000000000000000000000000";
    private const string PlayerInfoCapture =
        "0087002e1800008300000077000000000db803ed" +
        "00000000000000000000000000000000000000" +                            // 19 zeros
        "01" + "00000000000000000000000000000000" +                         // flag + 16 zeros
        "bd120000" +
        "0600640068007300690061006f00" +                                     // "dhsiao"
        "0000000000000000" +                                                 // 8 zeros
        "01" + "000000000000000000000000000000" +                           // flag + 15 zeros
        "0101" + "0000" + "88130000" +
        "000000000000000000000000000000000000000000000000000000000000000000"; // 33 zeros
    private const string AckCapture = "0014000e1800001000000004000000000004000100000000";

    private static string Frame(ushort msgId, byte[] payload)
    {
        using MemoryStream ms = new();
        DialectB.WriteFrame(ms, msgId, payload);
        return Convert.ToHexString(ms.ToArray()).ToLowerInvariant();
    }

    [Fact]
    public void SessionAuth_MatchesCapture() =>
        Assert.Equal(AuthCapture,
            Frame(SessionPackets.MsgSessionAuth, SessionPackets.SessionAuth("dhsiao510", 0x6C3D091C)));

    [Fact]
    public void PlayerInfo_MatchesCapture() =>
        Assert.Equal(PlayerInfoCapture,
            Frame(SessionPackets.MsgPlayerInfo, SessionPackets.PlayerInfo("dhsiao", 0x0DB803ED)));

    [Fact]
    public void Ack_MatchesCapture() =>
        Assert.Equal(AckCapture, Frame(SessionPackets.MsgAck, SessionPackets.Ack()));

    [Fact]
    public void DirectoryConnect_MatchesCapture() =>
        Assert.Equal("0000140000120012000000000100000000000037000000",
            Convert.ToHexString(DirectoryServer.ConnectPacket).ToLowerInvariant());

    [Fact]
    public async Task Framing_RoundTrips()
    {
        byte[] payload = SessionPackets.Ack();
        using MemoryStream ms = new();
        DialectB.WriteFrame(ms, SessionPackets.MsgAck, payload);
        ms.Position = 0;
        (ushort msgId, byte[] back) = await DialectB.ReadFrameAsync(ms);
        Assert.Equal(SessionPackets.MsgAck, msgId);
        Assert.Equal(payload, back);
    }

    [Fact]
    public void Envelope_LengthsScaleWithStringLength()
    {
        // A longer account id must grow outer/inner/sub lengths by 2 per extra char.
        byte[] a = SessionPackets.SessionAuth("dhsiao510", 1); // cookie 12 chars
        byte[] b = SessionPackets.SessionAuth("dhsiao5100", 1); // cookie 13 chars
        Assert.Equal(a.Length + 2, b.Length);
        int InnerLen(byte[] x) => (x[2] << 8) | x[3];
        Assert.Equal(InnerLen(a) + 2, InnerLen(b));
    }
}
