using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking
{
    /// <summary>
    /// Static iris console helper class.
    /// </summary>
    public static class IrisConsole
    {
        /// <summary>
        /// Iris verbosity levels.
        /// They will get used for limiting console output.
        /// </summary>
        public enum IrisVerbosity
        {
            NONE,
            ERRORS,
            DEBUG
        }

        public enum MessageType
        {
            INFO = 0,
            WARNING = 1,
            ERROR = 2,
            DEBUG = 3
        }

        /// <summary>
        /// The message types configuration struct.
        /// </summary>
        struct MessageTypeConfig
        {
            public MessageTypeConfig(ConsoleColor consoleColor)
            {
                this.consoleColor = consoleColor;
            }

            public ConsoleColor consoleColor;
        }

        private static MessageTypeConfig[] messageTypeConfigs = new MessageTypeConfig[]
        {
            new MessageTypeConfig(ConsoleColor.Gray),
            new MessageTypeConfig(ConsoleColor.Yellow),
            new MessageTypeConfig(ConsoleColor.Red),
            new MessageTypeConfig(ConsoleColor.Green)
        };

        /// <summary>
        /// Logs the given message from the given module as the given messagetype.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="module"></param>
        /// <param name="message"></param>
        public static void Log(MessageType type, string module, string message)
        {
            // Verbosity check
            if (IrisNetwork.verbosity == IrisVerbosity.NONE)
                return;
			if (IrisNetwork.verbosity == IrisVerbosity.ERRORS && type != MessageType.ERROR)
				return;

            ConsoleColor bkColor = Console.ForegroundColor;
            Console.ForegroundColor = messageTypeConfigs[(int)Convert.ChangeType(type, type.GetTypeCode())].consoleColor;

            Console.WriteLine("[" + DateTime.Now + "] - [" + module + "]: " + message);
            Console.ForegroundColor = bkColor;
        }
    }
}
