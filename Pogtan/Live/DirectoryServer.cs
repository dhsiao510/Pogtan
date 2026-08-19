using System.Net;
using System.Net.Sockets;

namespace Pogtan.Live;

/// <summary>
/// Directory/startup server (TCP 3830/3838). Verified dance:
///   1. S→C Connect packet, 23B plaintext (versions 18, headerType 0) — exact captured bytes.
///   2. C→S hello, 27B encrypted (logged, content ignored — byte 1 varies per run).
///   3. S→C blob, 1537B, static across days → replayed from Data/directory.blob.bin.
/// Afterwards the client either idles (cached channel data) or continues with the big
/// login fetch (observed once on 3838: 1308B up / 116KB down [NOT YET IMPLEMENTED —
/// currently logged only]).
/// </summary>
public class DirectoryServer(int port)
{
    /// <summary>Exact Connect packet captured 2026-08-18 (poptag.online, headerType 0).</summary>
    public static readonly byte[] ConnectPacket =
        Convert.FromHexString("0000140000120012000000000100000000000037000000");

    private readonly TcpListener listener = new(IPAddress.Any, port);

    public async Task Start()
    {
        try
        {
            listener.Server.NoDelay = true;
            listener.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DirectoryServer] BIND FAILED on {port}: {ex.Message}");
            throw;
        }
        Console.WriteLine($"[DirectoryServer] listening on {port}");
        while (true)
        {
            TcpClient tcp = await listener.AcceptTcpClientAsync();
            _ = Handle(tcp);
        }
    }

    private static string FindBlob()
    {
        foreach (string dir in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            string p = Path.Combine(dir, LiveConfig.BlobPath);
            if (File.Exists(p)) return p;
        }
        throw new FileNotFoundException($"directory blob not found (expected at {LiveConfig.BlobPath}; see Data/README.md)");
    }

    private async Task Handle(TcpClient tcp)
    {
        string ep = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
        Console.WriteLine($"[DirectoryServer:{port}] {ep} connected");
        try
        {
        using (tcp)
        {
            NetworkStream stream = tcp.GetStream();
            await stream.WriteAsync(ConnectPacket);

            byte[] hello = new byte[27];
            await stream.ReadExactlyAsync(hello);
            Console.WriteLine($"[DirectoryServer:{port}] <- hello: {Convert.ToHexString(hello)}");

            byte[] blob = await File.ReadAllBytesAsync(FindBlob());
            await stream.WriteAsync(blob);
            Console.WriteLine($"[DirectoryServer:{port}] -> blob ({blob.Length}B)");

            // Post-blob: log anything else (big login fetch / keepalives) until close.
            byte[] buf = new byte[8192];
            while (true)
            {
                int n = await stream.ReadAsync(buf);
                if (n == 0) throw new EndOfStreamException();
                Console.WriteLine($"[DirectoryServer:{port}] <- post-blob {n}B: {Convert.ToHexString(buf.AsSpan(0, Math.Min(n, 64)))}{(n > 64 ? "…" : "")}");
            }
        }
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine($"[DirectoryServer:{port}] {ep} disconnected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DirectoryServer:{port}] {ep} error: {ex.Message}");
        }
    }
}
