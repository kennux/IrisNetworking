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
        /// The message objects which will get awaited for sending.
        /// </summary>
        private Queue<AwaitedMessage> awaitingSendMessages = new Queue<AwaitedMessage>();

        /// <summary>
        /// Conatins all message types which should get ignored on receive.
        /// </summary>
        private List<Type> ignoredReceiveMessageTypes = new List<Type>();

        /// <summary>
        /// Conatins all message types which should get ignored on sending.
        /// </summary>
        private List<Type> ignoredSendMessageTypes = new List<Type>();

        /// <summary>
        /// Set this to tur to enforce strict receive packet order.
        /// If this is set to true and a packet arrives which is NOT the next awaited package, an assertion exception will get thrown.
        /// </summary>
        public bool StrictReceiveOrder = false;

        /// <summary>
        /// Set this to tur to enforce strict send packet order.
        /// If this is set to true and a packet arrives which is NOT the next awaited package, an assertion exception will get thrown.
        /// </summary>
        public bool StrictSendOrder = false;

        /// <summary>
        /// Enqueues a message of the given Type T to this message sequences receiving queue.
        /// This is returned for chaining.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageObject"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IrisTestMessageSequence AwaitReceive(Type messageType, Action<IrisNetworkMessage> callback)
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
        public IrisTestMessageSequence AddToReceiveIgnoreList(Type messageType)
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
            // If we already got everything we awaited, all is cool!
            if (this.awaitingReceiveMessages.Count == 0)
                return;

            // Get currently awaited message
            AwaitedMessage awaitedMessage = this.awaitingReceiveMessages.Peek();
            bool messageAwaited = awaitedMessage.messageType == message.GetType();
            bool messageIgnored = this.ignoredReceiveMessageTypes.Contains(message.GetType());

            // Message is ignored or the type we are awaiting!
            if (this.StrictReceiveOrder)
                Assert.IsTrue(messageIgnored || messageAwaited);

            // Callback
            if (messageAwaited)
            {
                awaitedMessage = this.awaitingReceiveMessages.Dequeue();
                awaitedMessage.callback(message);
            }
        }

        /// <summary>
        /// Enqueues a message of the given Type T to this message sequences receiving queue.
        /// This is returned for chaining.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageObject"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IrisTestMessageSequence AwaitSend(Type messageType, Action<IrisNetworkMessage> callback)
        {
            this.awaitingSendMessages.Enqueue(new AwaitedMessage(messageType, callback));
            return this;
        }

        /// <summary>
        /// Adds the given message type to the ignore-list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageObject"></param>
        /// <returns></returns>
        public IrisTestMessageSequence AddToSendIgnoreList(Type messageType)
        {
            this.ignoredSendMessageTypes.Add(messageType);
            return this;
        }

        /// <summary>
        /// Gets called if a message received.
        /// This function will perform some assertions in order to test if this message sequence can get validated.
        /// </summary>
        /// <param name="message"></param>
        public void SentMessage(IrisNetworkMessage message)
        {
            // If we already got everything we awaited, all is cool!
            if (this.awaitingSendMessages.Count == 0)
                return;

            // Get currently awaited message
            AwaitedMessage awaitedMessage = this.awaitingSendMessages.Peek();
            bool messageAwaited = awaitedMessage.messageType == message.GetType();
            bool messageIgnored = this.ignoredSendMessageTypes.Contains(message.GetType());

            // Message is ignored or the type we are awaiting!
            if (this.StrictSendOrder)
                Assert.IsTrue(messageIgnored || messageAwaited);

            // Callback
            if (messageAwaited)
            {
                awaitedMessage = this.awaitingSendMessages.Dequeue();
                awaitedMessage.callback(message);
            }
        }

        public void Validate()
        {
            Assert.IsTrue(this.awaitingSendMessages.Count == 0 && this.awaitingReceiveMessages.Count == 0);
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
