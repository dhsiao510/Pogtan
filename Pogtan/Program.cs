using Pogtan;
using Pogtan.Live;
using Pogtan.Server;

// Capture-verified real protocol by default (see bnb-notes docs/protocol-notes.md).
// The original skeleton servers (different regional build, unverified) run with --legacy.

if (args.Contains("--legacy"))
{
    LoginServer loginServer = new(ServerConfig.LoginPort);
    GameServer gameServer = new(ServerConfig.ChannelPort);
    Task.WaitAll(loginServer.Start(), gameServer.Start());
    return;
}

static int EnvPort(string name, int fallback) =>
    int.TryParse(Environment.GetEnvironmentVariable(name), out int p) ? p : fallback;

DirectoryServer directoryA = new(EnvPort("POGTAN_DIR_PORT_A", LiveConfig.DirectoryPortA));
DirectoryServer directoryB = new(EnvPort("POGTAN_DIR_PORT_B", LiveConfig.DirectoryPortB));
SessionServer session = new(EnvPort("POGTAN_SESSION_PORT", LiveConfig.SessionPort));

Task.WaitAll(directoryA.Start(), directoryB.Start(), session.Start());
