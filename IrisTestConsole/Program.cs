using System;
using System.Collections.Generic;
using IrisNetworking;
using IrisNetworking.Internal;
using IrisNetworking.Sockets;
using System.Net.Sockets;
using System.IO;

namespace IrisTestConsole
{
    static class IrisTestConsole
    {
        /// <summary>
        /// Gets returned by the test view in OwnershipRequest().
        /// </summary>
        public static bool OwnershipRequestAnswer = true;

        /// <summary>
        /// The iris server socket.
        /// </summary>
        private static IrisServerSocket serverSocket;

        /// <summary>
        /// The clients currently connected to the server socket.
        /// </summary>
        private static List<IrisClientSocket> serverClientSockets = new List<IrisClientSocket>();

        /// <summary>
        /// The iris client socket.
        /// </summary>
        private static IrisClientSocket clientSocket;

        /// <summary>
        /// The IrisMaster.
        /// </summary>
        private static TestManager manager = new TestManager();

        /// <summary>
        /// Interpretation function of the current shell.
        /// </summary>
        private static Action<string[]> interpretingFunction = null;

        public static void Main()
        {
            // Init iris
            IrisNetwork.verbosity = IrisConsole.IrisVerbosity.DEBUG;
            IrisNetwork.Multithread = true;
            manager = new TestManager();

            while (true)
            {
                // Read console input
                Console.Write("> ");
                string input = Console.ReadLine();
                Interpret(input);
            }
        }

        #region Main test console

        /// <summary>
        /// Main interpretation function.
        /// </summary>
        /// <param name="input"></param>
        private static void Interpret(string input)
        {
            string[] inputParts = input.Split(' ');

            switch (inputParts[0])
            {
                case "shell": // Change shell
                    if (inputParts.Length == 2)
                    {
                        if (inputParts[1] == "highlevel")
                            interpretingFunction = InterpretHighLevel;
                        else if (inputParts[1] == "lowlevel")
                            interpretingFunction = InterpretLowLevel;
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Given shell not found!");
                    }
                    else
                        IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: shell [SHELL - highlevel/lowlevel]");
                    break;
                case "exec": // Script execution
                    if (inputParts.Length == 2)
                    {
                        string scriptname = inputParts[1];

                        // Open script
                        if (File.Exists("scripts/" + scriptname + ".script"))
                        {
                            // Read lines from file
                            string[] lines = File.ReadAllLines("scripts/" + scriptname + ".script");

                            foreach (string i in lines)
                            {
                                Console.WriteLine("> " + i);
                                Interpret(i);
                            }
                        }
                    }
                    else
                        IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: exec []");
                    break;
                default:
                    if (interpretingFunction != null)
                        interpretingFunction(inputParts);
                    else
                        IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Command not found!");
                    break;
            }
        }

        #endregion

        #region High level test console

        /// <summary>
        /// The high level test console.
        /// </summary>
        public static void InterpretHighLevel(string[] inputParts)
        {
            // Interpret high level test command
            /*try
            {*/
                switch (inputParts[0])
                {
                    #region Dedicated client commands
                    case "dedicated_connect": // Connect to a dedicated server
                        if (inputParts.Length == 4)
                        {
                            // Get ip and port for connecting to them.
                            string ip = inputParts[1];
                            short port = short.Parse(inputParts[2]);
                            string playerName = inputParts[3];

                            IrisNetwork.LocalPlayerName = playerName;

                            IrisNetwork.ConnectDedicated(ip, port);

                            if (IrisNetwork.Connected)
                                IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Connected!");
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: connect [ip] [port] [playername]");
                        break;
                    case "disconnect": // Disconnects from everything
                        IrisNetwork.Disconnect();
                        break;
                    #endregion

                    #region Dedicated server commands

                    case "dedicated_start": // Starts a dedicated server
                        if (inputParts.Length == 4)
                        {
                            // Get ip and port for connecting to them.
                            string ip = inputParts[1];
                            short port = short.Parse(inputParts[2]);
                            short slots = short.Parse(inputParts[3]);

                            IrisNetwork.StartDedicated(ip, port, slots);

                            if (IrisNetwork.Connected)
                                IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Dedicated server started!");
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: dedicated_start [ip] [port] [slots]");
                        break;
                    case "dedicated_stop": // Stops the dedicated server
                        IrisNetwork.StopDedicated();
                        break;
                    #endregion

                    #region Initialization and Update

                    case "initialize": // Initializes Iris
                        IrisNetwork.Initialize(manager);
                        break;
                    case "update":
                        IrisNetwork.UpdateFrame();
                        break;


                    #endregion

                    #region Object commands
                    case "instantiate_object": // Instantiates an object
                        if (inputParts.Length == 3)
                        {
                            string objectName = inputParts[1];
                            string initialData = inputParts[2];
                            byte[] data = System.Text.ASCIIEncoding.UTF8.GetBytes(initialData);
                            IrisStream stream = new IrisStream(manager);
                            stream.Serialize(ref data);
                            IrisNetwork.InstantiateObject(objectName, stream.GetBytes());
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: instantiate_object [objectname] [initial data]");
                        break;
                    case "instantiate_objects": // Instantiates multiple objects
                        if (inputParts.Length == 4)
                        {
                            // Collect parameters
                            string objectName = inputParts[1];
                            string initialData = inputParts[2];
                            int objectCount = int.Parse(inputParts[3]);

                            // Prepare initial data
                            byte[] data = System.Text.ASCIIEncoding.UTF8.GetBytes(initialData);
                            IrisStream stream = new IrisStream(manager);
                            stream.Serialize(ref data);
                            data = stream.GetBytes();

                            Random rand = new Random();
                            for (int i = 0; i < objectCount; i++)
                            {
                                if (initialData == "randomized32")
                                {
                                    // Create random initial data
                                    byte[] randomData = new byte[32];
                                    rand.NextBytes(randomData);
                                    stream.Clear();
                                    stream.Serialize(ref randomData);
                                    data = stream.GetBytes();
                                }
                                IrisNetwork.InstantiateObject(objectName, data);
                            }
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: instantiate_objects [objectname] [initial data] [count]");
                        break;
                    case "destroy_object": // Destroys an object
                        if (inputParts.Length == 2)
                        {
                            int viewId = int.Parse(inputParts[1]);

                            IrisNetwork.DestroyObject(IrisNetwork.FindView(viewId));
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: destroy_object [viewId]");
                        break;
                    case "request_ownership":
                        if (inputParts.Length == 2)
                        {
                            int viewId = int.Parse(inputParts[1]);

                            IrisNetwork.RequestViewOwnership(IrisNetwork.FindView(viewId));
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: request_ownership [viewId]");
                        break;
                    case "ownership_request_answer":
                        if (inputParts.Length == 2)
                        {
                            OwnershipRequestAnswer = bool.Parse(inputParts[1]);
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: ownership_request_answer [true/false]");
                        break;
                    #endregion

                    #region RPC
                    // Remote Procedure Call commands

                    case "clear_rpcs": // View clear rpc
                        if (inputParts.Length == 2)
                        {
                            int viewId = int.Parse(inputParts[1]);

                            IrisNetwork.ClearRPCBuffer(IrisNetwork.FindView(viewId));
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: destroy_object [viewId]");
                        break;
                    case "rpc": // Normal rpc
                        if (inputParts.Length == 5)
                        {
                            int viewId = int.Parse(inputParts[1]);
                            string methodName = inputParts[2];
                            bool buffered = bool.Parse(inputParts[3]);
                            RPCTargets targets = (RPCTargets)Enum.Parse(typeof(RPCTargets), inputParts[4]);

                            IrisNetwork.RPC(IrisNetwork.FindView(viewId), targets, methodName, new object[1] { "test" }, buffered);
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: rpc [viewId] [Methodname] [buffered - true/false] [targets - Others/All]");
                        break;
                    case "rpc_player": // Player specific rpc
                        if (inputParts.Length == 4)
                        {
                            int viewId = int.Parse(inputParts[1]);
                            string methodName = inputParts[2];
                            int playerId = int.Parse(inputParts[3]);

                            IrisNetwork.RPC(IrisNetwork.FindView(viewId), new IrisPlayer[] { IrisNetwork.FindPlayer(playerId) }, methodName, new object[1] { "test" });
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: rpc_player [viewId] [Methodname] [playerId]");
                        break;
                    case "compression": // Compression setter
                        if (inputParts.Length == 2)
                        {
                            // Set compression flag
                            IrisNetwork.Compression = (IrisCompression)Enum.Parse(typeof(IrisCompression), inputParts[1]);
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: compression [NONE/GOOGLE_SNAPPY]");
                        break;
                    #endregion

                    #region Statistics
                    case "stats":
                        IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Bytes sent: " + IrisNetwork.BytesSent);
                        break;
                    #endregion

                    default:
                        IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Command unknown!");
                        break;
                } // Switch end
            /*}
            catch (Exception e)
            {
                IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Unhandled exception: " + e.Message + "\r\n\r\n" + e.StackTrace);
            }*/
        }

        #endregion

        #region Low level test console

        /// <summary>
        /// The low level test console.
        /// </summary>
        public static void InterpretLowLevel(string[] inputParts)
        {
            try
            {
                switch (inputParts[0])
                {
                    case "server_start":
                        // Arguments check
                        if (inputParts.Length == 3)
                        {
                            // Parse port argument
                            string bindIp = inputParts[1];
                            short port = short.Parse(inputParts[2]);

                            if (serverSocket != null)
                            {
                                IrisConsole.Log(IrisConsole.MessageType.WARNING, "IrisTestConsole", "There's already an iris test server running. It will get closed and overwritten.");
                                serverSocket.Close();
                            }

                            // Create server socket.
                            serverSocket = new IrisServerSocket(bindIp, port, ServerConnectionAccept);
                            serverClientSockets.Clear();

                            IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Server socket started!");
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: start_server [bind-ip] [port]");
                        break;
                    case "server_status":
                        // Print main status
                        IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Sockets connected to the server: " + serverClientSockets.Count);

                        string connectedClients = "";
                        foreach (IrisClientSocket socket in serverClientSockets)
                        {
							connectedClients += socket.Socket.RemoteEndPoint + ",";
                        }
                        connectedClients = connectedClients.Substring(0,connectedClients.Length-1);
                        IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Connected clients: " + connectedClients);
                        break;
                    case "client_connect":
                        // Arguments check
                        if (inputParts.Length == 3)
                        {
                            // Parse port argument
                            string ip = inputParts[1];
                            short port = short.Parse(inputParts[2]);

                            if (clientSocket != null)
                            {
                                IrisConsole.Log(IrisConsole.MessageType.WARNING, "IrisTestConsole", "The current iris client is already existing. It will get closed and overwritten.");
                                clientSocket.Close();
                            }

                            // Create server socket.
                            clientSocket = new IrisClientSocket(ip, port, ClientDataArrived, ClientDisconnect);
                            serverClientSockets.Clear();
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: connect_client [ip] [port]");
                        break;
                    case "client_send_string":
                        // Arguments check
                        if (inputParts.Length == 2)
                        {
                            string str = inputParts[1];

                            if (clientSocket != null && !clientSocket.Connected)
                            {
                                IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Can't send data without being connected to anything");
                                return;
                            }

                            // Get utf8 bytes from string.
                            byte[] data = System.Text.ASCIIEncoding.UTF8.GetBytes(str);
                            clientSocket.SendRaw(data);
                        }
                        else
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "USAGE: send_string [string]");
                        break;
                    case "client_disconnect":
                        if (clientSocket == null)
                        {
                            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Can't disconnect client without connection!");
                        }

                        clientSocket.Close();
                        break;
                    default:
                        IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Command unknown!");
                        break;
                } // Switch end
            }
            catch (Exception e)
            {
				IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Unhandled exception: \r\n" + e.Message + "\r\n" + e.StackTrace);
            }
        }

        #region Events

        private static void ServerConnectionAccept(Socket sck)
        {
            IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Accepted connection on server socket from: " + sck.RemoteEndPoint );
            serverClientSockets.Add(new IrisClientSocket(sck, ServerDataArrived, ServerClientConnectionDropped));
        }

        private static void ServerDataArrived(IrisClientSocket.PacketInformation packetInformation)
        {
            IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Got message from client (" + packetInformation.client.Socket.RemoteEndPoint + "): \r\n" + System.Text.ASCIIEncoding.UTF8.GetString(packetInformation.payload));
        }

        private static void ServerClientConnectionDropped(IrisClientSocket client)
        {
            serverClientSockets.Remove(client);
            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Client connection got dropped: " + client.Socket.RemoteEndPoint);
        }

        private static void ClientDataArrived(IrisClientSocket.PacketInformation packetInformation)
        {
            IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisTestConsole", "Got message from server (" + packetInformation.client.Socket.RemoteEndPoint + "): \r\n" + System.Text.ASCIIEncoding.UTF8.GetString(packetInformation.payload));
        }

        private static void ClientDisconnect(IrisClientSocket client)
        {
            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisTestConsole", "Connection got dropped from server.");
        }

        #endregion

        #endregion
    }
}
