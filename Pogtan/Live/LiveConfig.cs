namespace Pogtan.Live;

/// <summary>
/// Constants verified against live captures of the poptag.online reference server
/// (2026-08-18; see bnb-notes docs/protocol-notes.md).
/// The 2010 global client talks TWO protocol dialects:
///   A) directory/startup on TCP 3830/3838 — Pogtan-style [00][u16be len][payload]
///      for the Connect packet only, then a fixed encrypted dance
///      (27B client hello, 1537B static server blob, replayed from Data/).
///   B) session on TCP 47611 — [u16be outerLen][u16be msgid][payload];
///      envelope [18 00][u16be innerLen][u8 tag][u24be subLen];
///      S2C plaintext, UTF-16LE counted strings. See DialectB / SessionPackets.
/// </summary>
public static class LiveConfig
{
    /// <summary>Client build reports 18 (Pogtan's 37 is a different regional build).</summary>
    public const int GameVersion = 18;

    /// <summary>multisvr.ssd rotation port (verified in captures).</summary>
    public const int DirectoryPortA = 3830;

    /// <summary>multisvr.ssd configured port (verified in captures). 3834 is dead upstream — do not bind it.</summary>
    public const int DirectoryPortB = 3838;

    /// <summary>Session/game TCP port (verified; client opens 3 parallel connections at login).</summary>
    public const int SessionPort = 47611;

    /// <summary>
    /// Bring-up identity. The 47611 auth request carries an encrypted block we cannot
    /// decrypt yet [UNRESOLVED], so the account cannot be read from c2s — serve this
    /// reference identity until the crypto falls.
    /// </summary>
    public static string AccountId = "dhsiao510";
    public static string DisplayName = "dhsiao";

    /// <summary>Static 1537B directory blob, capture-replayed (gitignored binary, see Data/README.md).</summary>
    public const string BlobPath = "Data/directory.blob.bin";
}
