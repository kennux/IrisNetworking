using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This is the static iris packet identifier.
    /// It gets used to identify and serialize iris packets based on their header and payload.
    /// </summary>
    public static class IrisPacketIdentifier
    {
        private static Dictionary<int, Type> serverToClientMessages = new Dictionary<int, Type>();
        private static Dictionary<int, Type> clientToServerMessages = new Dictionary<int, Type>();

        /// <summary>
        /// Registers all packages by running through all classes via reflection.
        /// </summary>
        public static void Bootstrap()
        {
            serverToClientMessages.Clear();
            clientToServerMessages.Clear();

            // Find all packets
            // Define base type
            System.Type baseType = typeof(IrisNetworkMessage);

            // Load all subclasses of MonoBehaviour 
            List<System.Type> result = new List<System.Type>();
            Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();

            // Now we iterate through every assembly and get all iris network messages.
            foreach (System.Reflection.Assembly A in AS)
            {
                // Check if classes are subclass of the basetype
                System.Type[] types = A.GetTypes();
                foreach (var T in types)
                {
                    if (T.IsSubclassOf(baseType))
                        result.Add(T);
                }
            }

            foreach (System.Type t in result)
            {
                // Skip non-instantiable types
                if (!t.IsAbstract && !t.IsInterface)
                {
                    IrisNetworkMessage m = (IrisNetworkMessage)Activator.CreateInstance(t);
                    // Is it a server message?
                    if (t.IsSubclassOf(typeof(IrisServerToClientMessage)))
                        serverToClientMessages.Add(m.GetPacketId(), t);
                    else
                        clientToServerMessages.Add(m.GetPacketId(), t);
                }
            }
        }

        /// <summary>
        /// Returns a new instance of a message object for the given header.
        /// Will return null if the message was not found.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static IrisNetworkMessage GetServerToClientMessage(byte header)
        {
            if (!serverToClientMessages.ContainsKey((int)header))
                return null;
            return (IrisNetworkMessage)Activator.CreateInstance(serverToClientMessages[header]);
        }

        /// <summary>
        /// Returns a new instance of a message object for the given header.
        /// Will return null if the message was not found.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static IrisNetworkMessage GetClientToServerMessage(byte header)
        {
            if (!clientToServerMessages.ContainsKey((int)header))
                return null;
            return (IrisNetworkMessage)Activator.CreateInstance(clientToServerMessages[header]);
        }
    }
}
