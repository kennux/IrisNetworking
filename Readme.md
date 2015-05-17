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

Thread-safety
======================

Iris Networking was developed with multithreading in mind.
It uses synchronous sockets whose receive and send queue handlers are offloaded to seperate threads.

You can set IrisNetwork.Multithread to false to prevent those threads from actually interpreting arriving data.
If you do this, all interpretation will be done in IrisNetwork.UpdateFrame(). It is your responsibility now to take care of calling it.

If you set it to true, incoming messages will get interpreted on the receiver thread which will be a unique thread for every socket.
So you need to make sure that every base class or interface you use is implemented with thread-safety in mind.

Unity implementation
======================

The unity implementation of iris networking mainly serves the purpose of a reference implementation.
There are some tests and examples in it that demonstrate how it works.

The following examples will need assets from the assetstore to work (You may also have to mod them a little bit):
- EVP5MP - Uses Edy's Vehicle Physics for Unity 5 and adds IrisNetworking based multiplayer sync.