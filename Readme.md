IrisNetworking
======================

Iris Networking is a networking engine mainly developed for games.
It supports the following features so far:

- TCP Protocol
- Compression (using Google's Snappy)
- Object spawning, despawning and synchronization (view-based)
- RPC-Calls
- RPC-Buffering
- Dedicated Servers
- Unity implementation (Still WIP)
- View takeover

Planned features:

- Master server for Dedicated Servers
- PhotonCloud like Cloud functions

This engine is still in alpha-stage and not ready for productional use!

Unity implementation
======================

The unity implementation of iris networking mainly serves the purpose of a reference implementation.
There are some tests and examples in it that demonstrate how it works.

The following examples will need assets from the assetstore to work (You may also have to mod them a little bit):
- EVP5MP - Uses Edy's Vehicle Physics for Unity 5 and adds IrisNetworking based multiplayer sync.