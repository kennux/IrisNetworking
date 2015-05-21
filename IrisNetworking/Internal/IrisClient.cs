using System;
using System.Collections.Generic;
using IrisNetworking.Sockets;
using System.Net.Sockets;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Abstract iris client implementation.
    /// It handles some basic stuff like
    /// - Packet handling
    /// </summary>
    public abstract class IrisClient
    {
        /// <summary>
        /// Forwards the connected flag of the underlying client socket.
        /// </summary>
        public bool Connected
        {
            get
            {
                return this.clientSocket != null && this.clientSocket.Connected;
            }
        }

        /// <summary>
        /// The iris client socket used for data receiving and sending.
        /// </summary>
        public IrisClientSocket ClientSocket
        {
            get { return this.clientSocket; }
        }
        protected IrisClientSocket clientSocket;

        /// <summary>
        /// Gets the player object for this client.
        /// Will always be IrisNetwork.LocalPlayer for non-dedicated!
        /// </summary>
        public IrisPlayer Player
        {
            get
            {
                return this.player;
            }
        }
        protected IrisPlayer player;

        /// <summary>
        /// Forwards IrisClientSocket.BytesSent.
        /// </summary>
        public int BytesSent
        {
            get { return this.clientSocket.BytesSent; }
        }

        /// <summary>
        /// The packet queue used for offloading packet interpretation on mainthread if IrisNetwork.Multithread is false.
        /// </summary>
        private Queue<IrisClientSocket.PacketInformation> packetQueue = new Queue<IrisClientSocket.PacketInformation>();
        private object packetQueueLockObject = new object();

        /// <summary>
        /// The iris client master.
        /// </summary>
        protected IrisMaster master;

        /// <summary>
        /// Sends a message directly to the connected client.
        /// Never use this in combination with the High-Level Game networking api or you'll fuck everything up!
        /// </summary>
        /// <param name="message"></param>
        public virtual void SendMessage(IrisNetworkMessage message)
        {
            IrisStream stream = new IrisStream(this.master);
            byte id = message.GetPacketId();
            stream.Serialize(ref id);
            message.Serialize(stream);
            this.clientSocket.SendRaw(stream.GetBytes());
        }

        /// <summary>
        /// Closes the underlying iris client socket.
        /// </summary>
        public void Close()
        {
            this.clientSocket.Close();
        }

        /// <summary>
        /// Updates this socket.
        /// This will only do work if IrisNetwork.Multithread is false.
        /// </summary>
        public virtual void Update()
        {
            lock (this.packetQueueLockObject)
                while (this.packetQueue.Count > 0)
                    this.InterpretPacket(this.packetQueue.Dequeue());
        }

        protected abstract void HandlePacket(IrisStream stream, byte header);


        /// <summary>
        /// Receive packet handler.
        /// Gets called from the IrisClientSocket.
        /// </summary>
        /// <param name="p"></param>
        protected void ReceivePacket(IrisClientSocket.PacketInformation p)
        {
            lock (this.packetQueueLockObject)
            {
                this.packetQueue.Enqueue(p);
            }
        }

        /// <summary>
        /// Packet interpretation handler.
        /// </summary>
        /// <param name="p"></param>
        protected virtual void InterpretPacket(IrisClientSocket.PacketInformation p)
        {
            // Validity check
            if (p.payload.Length < 1 || !this.clientSocket.Connected)
                return;

            // Get packet header
            byte header = p.payload[0];
            byte[] data = new byte[p.payload.Length - 1];

            // Copy everything except the packet header
            Array.Copy(p.payload, 1, data, 0, data.Length);

            // Create iris stream
            IrisStream stream = new IrisStream(this.master, data);

			try
			{
            	this.HandlePacket(stream, header);
			}
			catch (SerializationException e)
			{
				IrisConsole.Log (IrisConsole.MessageType.INFO, "IrisClient", "Message serialization thrown exception: " + e.Message + "\r\n\r\n");
			}
        }

        /// <summary>
        /// Gets called from the underlying iris client socket if the user or server dropped the connection.
        /// </summary>
        /// <param name="socket"></param>
        protected virtual void ClientDisconnected(IrisClientSocket socket)
        {
        }
    }
}
