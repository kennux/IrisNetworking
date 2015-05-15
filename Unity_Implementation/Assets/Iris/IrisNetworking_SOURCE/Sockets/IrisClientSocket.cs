﻿CPusing System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IrisNetworking.Sockets
{
    /// <summary>
    /// The iris client socket base class.
    /// Iris sockets are the most low-level socket implementation just implementing basic packet sending between server and client.
    /// It can only send byte-arrays of array, but handles reliability and data validation.
    /// This class is fully thread-safe.
    /// </summary>
    public class IrisClientSocket
    {
        /// <summary>
        /// Forwards the Socket.Connected property.
        /// </summary>
        public bool Connected
        {
            // TODO: Connection state determination
            get { return this.client != null && this.client.Connected; }
        }

        /// <summary>
        /// Returns the underlying raw c# socket.
        /// </summary>
        public Socket Socket
        {
            get { return this.client;  }
        }

        /// <summary>
        /// The socket instance used for data sending / retrieval.
        /// </summary>
        protected Socket client;

        /// <summary>
        /// The thread used for sending data to the remote end.
        /// </summary>
        protected Thread sendingThread;

        /// <summary>
        /// The thread used for reading from a socket.
        /// </summary>
        protected Thread receivingThread;

        /// <summary>
        /// The data received handler.
        /// </summary>
        protected Action<PacketInformation> dataReceivedHandler;

        /// <summary>
        /// This handler will get called after a socket was disconnected.
        /// </summary>
        private Action<IrisClientSocket> disconnectHandler;

        /// <summary>
        /// The message buffer used in StartBuffer() / StopBuffer().
        /// </summary>
        private List<byte[]> messageBuffer = null;

        #region Construction functions

        /// <summary>
        /// Constructs a new iris socket as client.
        /// 
        /// This function may throws exceptions from the underlying System.Net.Sockets.Socket.
        /// </summary>
        /// <param name="ip">The ip-adress of the remote end of this connection</param>
        /// <param name="port">The port on the remote end of this connection</param>
        public IrisClientSocket(String ip, short port, Action<PacketInformation> dataReceivedHandler, Action<IrisClientSocket> disconnectHandler)
        {
            IPHostEntry ipHostInfo = new IPHostEntry();
            ipHostInfo.AddressList = new IPAddress[] { IPAddress.Parse(ip) };
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            Socket socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect
            socket.Connect(remoteEP);

            // Init
            this.Initialize(socket, dataReceivedHandler, disconnectHandler);
        }

        /// <summary>
        /// Creates an iris socket from the given socket.
        /// This will just set the client socket reference, start read/write threads and won't do any other initialization.
        /// </summary>
        /// <param name="socket"></param>
        public IrisClientSocket(Socket socket, Action<PacketInformation> dataReceivedHandler, Action<IrisClientSocket> disconnectHandler)
        {
            this.Initialize(socket, dataReceivedHandler, disconnectHandler);
        }

        /// <summary>
        /// Initializes this client socket.
        /// This function must get called in every constructor!
        /// </summary>
        /// <param name="socket"></param>
        private void Initialize(Socket socket, Action<PacketInformation> dataReceivedHandler, Action<IrisClientSocket> disconnectHandler)
        {
            this.client = socket;

            // Check if socket is connected.
            if (!this.client.Connected)
            {
                // Throw connection failed exception
                IPEndPoint ipEndPoint = socket.RemoteEndPoint as IPEndPoint;
                throw new ConnectionFailedException(ipEndPoint.Address.ToString(), (short)ipEndPoint.Port);
            }

            this.dataReceivedHandler = dataReceivedHandler;
            this.disconnectHandler = disconnectHandler;

            // Initialize threads
            this.sendingThread = new Thread(new ThreadStart(this.SenderThread));
            this.receivingThread = new Thread(new ThreadStart(this.ReceiverThread));
            this.sendingThread.Start();
            this.receivingThread.Start();
        }

        #endregion

        #region I/O Functions

        /// <summary>
        /// Sends an array of bytes to the connection remote end point asynchronously.
        /// </summary>
        /// <param name="data"></param>
        public void SendRaw(byte[] data)
        {
            if (this.messageBuffer != null)
            {
                // Buffering active
                this.messageBuffer.Add(data);
                return;
            }

            lock (this.sendingQueueLockObject)
            {
                this.sendingQueue.Enqueue(data);
            }
        }

        #endregion

        #region Thread functions

        #region Data queues and queue structs

        // Queue data class used to represent an element in the receive and send queue.
        public class PacketInformation
        {
            public PacketInformation(byte[] payload, IrisClientSocket client)
            {
                this.payload = payload;
                this.client = client;
            }

            public byte[] payload;
            public IrisClientSocket client;
        }

        /// <summary>
        /// The data in the queue used for sending.
        /// </summary>
        private Queue<byte[]> sendingQueue = new Queue<byte[]>();

        // Lock objects for the queues
        private object sendingQueueLockObject = new object();

        #endregion

        #region Buffering

        /// <summary>
        /// This can get used to start message sending buffering.
        /// If this is enabled, messages submitted to SendMessage() will get saved in an internal buffer.
        /// This buffer will get flushed after calling StopBuffering().
        /// </summary>
        public void StartBuffering()
        {
            // If currently buffered, do noting
            if (this.messageBuffer != null)
                return;

            // Init message buffer
            this.messageBuffer = new List<byte[]>();
        }

        /// <summary>
        /// Stops the buffering started in StartBuffering().
        /// </summary>
        public void StopBuffering()
        {
            lock (this.sendingQueueLockObject)
            {
                if (this.messageBuffer == null)
                    return;

                // Process all queue data objects
                foreach (byte[] d in this.messageBuffer)
                {
                    this.sendingQueue.Enqueue(d);
                }

                // Buffering done
                this.messageBuffer = null;
            }
        }

        #endregion

        /// <summary>
        /// The delay used to check for work in the queues.
        /// </summary>
        private const int THREAD_WORK_CHECK_DELAY = 10;

        /// <summary>
        /// This is the sender thread's main function.
        /// It will run till this socket got disconnected.
        /// Once disconnected, the thread will get stopped.
        /// </summary>
        /// <param name="result"></param>
        private void SenderThread()
        {
            // Temporary queue for 
            Queue<byte[]> temporaryQueue = new Queue<byte[]>();

            while (this.Connected)
            {
                // Process sending queue
                lock (this.sendingQueueLockObject)
                {
                    // Clear temporary data for filling in the next step
                    temporaryQueue.Clear();

                    // Create copy of all items in the sending queue
                    while (this.sendingQueue.Count > 0)
                    {
                        temporaryQueue.Enqueue(this.sendingQueue.Dequeue());
                    }
                }

                if (temporaryQueue.Count > 0)
                {
                    IrisStream stream = new IrisStream(null);

                    // Process all queue data objects
                    foreach (byte[] d in temporaryQueue)
                    {
                        byte[] data = d;

                        // Buffer the payload
                        stream.Serialize(ref data);
                    }

                    byte[] payload = stream.GetBytes();

                    // Check if compression is enabled
                    if (IrisNetwork.Compression != IrisCompression.NONE)
                    {
                        switch (IrisNetwork.Compression)
                        {
                            case IrisCompression.GOOGLE_SNAPPY:

                                long ticks = System.DateTime.Now.Ticks;

                                // Compress using google snappy
                                Snappy.Sharp.SnappyCompressor compressor = new Snappy.Sharp.SnappyCompressor();
                                byte[] compressed = new byte[compressor.MaxCompressedLength(payload.Length)];
                                int compressedLength = compressor.Compress(payload, 0, payload.Length, compressed);

                                payload = new byte[compressedLength];

                                Array.Copy(compressed, payload, payload.Length);

                                long elapsedTicks = System.DateTime.Now.Ticks - ticks;
                                float elapsedMilliseconds = (elapsedTicks * 10) * 0.000001f;
                                // Output statistics
                                IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "Sent out network packet with compression. Uncompressed size: " + compressed.Length + ", Compressed size: " + payload.Length + ", Benchmark (ms): " + elapsedMilliseconds);
                                break;
                        }
                    }

                    this.client.Send(BitConverter.GetBytes(payload.Length));

                    this.client.Send(payload);
                }

                Thread.Sleep(THREAD_WORK_CHECK_DELAY);
            }

            this.Close();
        }

        /// <summary>
        /// Receives data till the given count of bytes is received.
        /// </summary>
        private byte[] ReceiveReliable(int count)
        {
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            int bytesReceived = 0;

            while (bytesReceived < count)
            {
                // Prepare buffer
                int needsBytes = count - bytesReceived;
                byte[] buffer = new byte[needsBytes];

                // Read data
                int actuallyRead = this.client.Receive(buffer);
                bytesReceived += actuallyRead;
                memoryStream.Write(buffer, 0, actuallyRead);
            }

            return memoryStream.ToArray();
        }

        /// <summary>
        /// This is the receiver thread's main function.
        /// It will run till this socket got disconnected.
        /// Once disconnected, the thread will get stopped.
        /// </summary>
        /// <param name="result"></param>
        private void ReceiverThread()
        {
            while (this.Connected)
            {
                try
                {
                    // Read packet header
                    byte[] header = this.ReceiveReliable(4);

                    // Create the packet payload length from header field
                    int payloadLength = BitConverter.ToInt32(header, 0);

                    // Get payload
                    byte[] payload = this.ReceiveReliable(payloadLength);

                    // Check if compression is enabled
                    if (IrisNetwork.Compression != IrisCompression.NONE)
                    {
                        switch (IrisNetwork.Compression)
                        {
                            case IrisCompression.GOOGLE_SNAPPY:
                                // Decompress using google snappy
                                Snappy.Sharp.SnappyDecompressor decompressor = new Snappy.Sharp.SnappyDecompressor();
                                byte[] decompressed = decompressor.Decompress(payload, 0, payload.Length);
                                payload = decompressed;

                                break;
                        }
                    }

                    // Process data
                    // Read data from stream
                    IrisStream stream = new IrisStream(null, payload);

                    // Interpret stream data
                    while (!stream.EndReached)
                    {
                        byte[] data = null;
                        stream.Serialize(ref data);

                        this.dataReceivedHandler(new PacketInformation(data, this));
                    }
                   
                    Thread.Sleep(THREAD_WORK_CHECK_DELAY);
                }
                catch (SocketException e)
                {
                    // TODO: Error handling
                }
            }

            this.Close();
        }

        #endregion

        #region Misc functions

        private volatile bool closed = false;

        /// <summary>
        /// Closes this socket's resources, stops threads and kills the connection.
        /// </summary>
        public void Close()
        {
            if (!this.closed)
            {
                this.closed = true;
				this.disconnectHandler(this);
				
				this.receivingThread.Interrupt ();
				this.sendingThread.Interrupt ();
				this.receivingThread.Abort ();
				this.sendingThread.Abort ();

                if (this.Connected)
                    this.client.Close();
            }
        }

        #endregion
    }
}