# Data — capture-replayed binary assets

`directory.blob.bin` (1537 B) is the **static directory blob** the reference server
sends after the 27-byte client hello on TCP 3830/3838. It is byte-identical across
days (verified 2026-08-18), so replaying it is legitimate until the encryption is
understood. It is **gitignored** (encrypted third-party server data) — on a fresh
clone, extract it from a capture:

```sh
# from workspace/captures (any startup capture works):
tail -c +24 mitm-<ts>-<tag>-tcp3830-1.s2c.bin > Pogtan/Pogtan/Data/directory.blob.bin
```

Without it the DirectoryServer answers the Connect packet and then fails the blob
send (client will sit at startup); the session server on 47611 is unaffected.
