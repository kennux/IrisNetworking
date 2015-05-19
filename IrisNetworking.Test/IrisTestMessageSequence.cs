using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrisNetworking.Test
{
    public class IrisTestMessageSequence
    {

        private class AwaitedMessage
        {
            public Action<IrisNetworkMessage> callback;
            public Type messageType;

            public AwaitedMessage(Type messageType, Action<IrisNetworkMessage> callback)
            {
                this.messageType = messageType;
                this.callback = callback;
            }
        }

        /// <summary>
        /// The message objects which will get awaited for receiving.
        /// </summary>
        private Queue<AwaitedMessage> awaitingReceiveMessages = new Queue<AwaitedMessage>();

        /// <summary>
        /// Conatins all message types which should get ignored on receive.
        /// </summary>
        private List<Type> ignoredReceiveMessageTypes = new List<Type>();


        /// <summary>
        /// Enqueues a message of the given Type T to this message sequences receiving queue.
        /// This is returned for chaining.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageObject"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IrisTestMessageSequence AwaitReceive (Type messageType, Action<IrisNetworkMessage> callback) where T : IrisNetworkMessage
        {
            this.awaitingReceiveMessages.Enqueue(new AwaitedMessage(messageType, callback));
            return this;
        }

        /// <summary>
        /// Adds the given message type to the ignore-list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageObject"></param>
        /// <returns></returns>
        public IrisTestMessageSequence AddToReceiveIgnoreList (Type messageType)
        {
            this.ignoredReceiveMessageTypes.Add(messageType);
            return this;
        }

        /// <summary>
        /// Gets called if a message received.
        /// This function will perform some assertions in order to test if this message sequence can get validated.
        /// </summary>
        /// <param name="message"></param>
        public void ReceivedMessage(IrisNetworkMessage message)
        {
            // Get currently awaited message
            AwaitedMessage awaitedMessage = this.awaitingReceiveMessages.Peek();
            bool messageIgnored = this.ignoredReceiveMessageTypes.Contains(message.GetType());

            // Message is ignored or the type we are awaiting!
            Assert.IsTrue(messageIgnored || awaitedMessage.messageType == message.GetType());

            // Callback
            awaitedMessage = this.awaitingReceiveMessages.Dequeue();
            awaitedMessage.callback(message);
        }

        /// <summary>
        /// Clears all awaiting buffers.
        /// </summary>
        public void Clear()
        {
            this.awaitingReceiveMessages.Clear();
            this.ignoredReceiveMessageTypes.Clear();
        }
    }
}
