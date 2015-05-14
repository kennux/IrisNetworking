using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IrisNetworking.Sockets
{
    /// <summary>
    /// 
    /// </summary>
    public class IrisServerSocket
    {
        /// <summary>
        /// The socket instance used for listening.
        /// </summary>
        protected Socket server;

        /// <summary>
        /// The is running flag, will get set to true in the constructor and false in Close().
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// The listener thread which will run ListenerThreadFunction().
        /// </summary>
        private Thread listenerThread;

        /// <summary>
        /// This handler will get called after a socket was accepted by this server socket.
        /// </summary>
        private System.Action<Socket> acceptHandler;

        #region Construction functions

        /// <summary>
        /// Constructs a new iris server socket.
        /// 
        /// This function may throws exceptions from the underlying System.Net.Sockets.Socket.
        /// </summary>
        /// <param name="ip">The ip-adress to use as bind address</param>
        /// <param name="port">The port to listen on.</param>
        public IrisServerSocket(String ip, short port, System.Action<Socket> acceptHandler)
        {
            IPHostEntry ipHostInfo = new IPHostEntry();
            ipHostInfo.AddressList = new IPAddress[] { IPAddress.Parse(ip) };
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEP = new IPEndPoint(ipAddress, port);
            this.acceptHandler = acceptHandler;

            // Create a TCP/IP socket.
            Socket socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind
            socket.Bind(localEP);
            socket.Listen(10);

            // Init
            this.Initialize(socket);
        }

        /// <summary>
        /// Initializes this client socket.
        /// This function must get called in every constructor!
        /// </summary>
        /// <param name="socket"></param>
        private void Initialize(Socket socket)
        {
            this.server = socket;

            // Init listener
            this.isRunning = true;
            this.listenerThread = new Thread(new ThreadStart(this.ListenerThreadFunction));
            this.listenerThread.Start();
        }

        #endregion

        #region Listener functions

        /// <summary>
        /// Will run till the server gets killed and accepts connections.
        /// </summary>
        private void ListenerThreadFunction()
        {
            while (this.isRunning)
            {
                Socket socket = this.server.Accept();
                this.acceptHandler(socket);
            }
        }

        #endregion

        #region Misc functions

        /// <summary>
        /// Closes this socket's resources, stops threads and kills the connection.
        /// </summary>
        public void Close()
        {
            this.server.Close();
        }

        #endregion
    }
}
