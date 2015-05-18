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
- Latency measuring (WIP)

Planned features:

- Master server for Dedicated Servers
- PhotonCloud like Cloud functions

This engine is still in alpha-stage and not ready for productional use!

Usage
======================

How to implement?!
----------------------

You need to implement your own IrisView and IrisMaster classes.
They represent the interface from your engine / game to iris networking.
Also, there's a update function which should get called continously.

IrisNetworking.UpdateFrame() should get called everytime you want to interpret incoming packages.
This function will also send out a partial (on client) and a complete frame update on the server.
So, normally you got a network update-rate of like 20 times a second, so you need to make sure your main-thread calls IrisNetworking.UpdateFrame() every 50ms.

Thread-safety
----------------------

Iris Networking was developed with multithreading in mind.
It uses synchronous sockets whose receive and send queue handlers are offloaded to seperate threads.

However, i wanted to keep thread-synchronization efforts to a minimum level, so all logic will run on the main thread.
Multithreading will just get used to send out pings, receive data and for flushing the send buffer.

Unity implementation
----------------------

The unity implementation of iris networking mainly serves the purpose of a reference implementation.
There are some tests and examples in it that demonstrate how it works.

The following examples will need assets from the assetstore to work (You may also have to mod them a little bit):
- EVP5MP - Uses Edy's Vehicle Physics for Unity 5 and adds IrisNetworking based multiplayer sync.