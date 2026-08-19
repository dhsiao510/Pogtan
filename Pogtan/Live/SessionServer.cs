using System.Net;
using System.Net.Sockets;
using Pogtan.Util;

namespace Pogtan.Live;

/// <summary>
/// Session server (TCP 47611). Verified behavior: the client opens 3 parallel
/// connections at login, each carrying exactly one request/response exchange
/// (0x0b auth, 0x2d player info, 0x0d ack), then keeps #1/#2 open and drops #3.
/// Requests carry an encrypted block we accept-and-log [UNRESOLVED] — bring-up
/// identity comes from LiveConfig until the crypto falls.
/// </summary>
public class SessionServer(int port)
{
    private readonly TcpListener listener = new(IPAddress.Any, port);
    private int nextSessionId = 0x6C3D0000; // cosmetic: matches captured sessionId high bytes

    public async Task Start()
    {
        try
        {
            listener.Server.NoDelay = true;
            listener.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SessionServer] BIND FAILED on {port}: {ex.Message}");
            throw;
        }
        Console.WriteLine($"[SessionServer] listening on {port}");
        while (true)
        {
            TcpClient tcp = await listener.AcceptTcpClientAsync();
            _ = Handle(tcp);
        }
    }

    private async Task Handle(TcpClient tcp)
    {
        uint sessionId = (uint)Interlocked.Add(ref nextSessionId, 0x111);
        string ep = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
        Console.WriteLine($"[SessionServer] {ep} connected, sessionId={sessionId:X8}");
        try
        {
        using (tcp)
        {
            NetworkStream stream = tcp.GetStream();
            while (true)
            {
                (ushort msgId, byte[] payload) = await DialectB.ReadFrameAsync(stream);
                Console.WriteLine($"[SessionServer] <- {msgId:X4} ({payload.Length + 4}B): {Convert.ToHexString(payload)}");
                switch (msgId)
                {
                    case SessionPackets.MsgSessionAuthRequest:
                        DialectB.WriteFrame(stream, SessionPackets.MsgSessionAuth,
                            SessionPackets.SessionAuth(LiveConfig.AccountId, sessionId));
                        break;
                    case SessionPackets.MsgPlayerInfoRequest:
                        DialectB.WriteFrame(stream, SessionPackets.MsgPlayerInfo,
                            SessionPackets.PlayerInfo(LiveConfig.DisplayName, sessionId));
                        break;
                    case SessionPackets.MsgAckRequest:
                        DialectB.WriteFrame(stream, SessionPackets.MsgAck, SessionPackets.Ack());
                        break;
                    default:
                        Console.WriteLine($"[SessionServer] !! unhandled msgid {msgId:X4} — no reply");
                        break;
                }
            }
        }
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine($"[SessionServer] {ep} disconnected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SessionServer] {ep} error: {ex.Message}");
        }
    }
}
