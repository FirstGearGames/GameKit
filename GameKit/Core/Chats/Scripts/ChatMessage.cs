using FishNet.Connection;

namespace GameKit.Core.Chats
{

    public struct ChatMessage
    {
        /// <summary>
        /// Type of message sent or received.
        /// </summary>
        public ushort MessageType;
        /// <summary>
        /// Client receiving the message. This value may be null if not a direct message.
        /// </summary>
        public NetworkConnection Receiver;
        /// <summary>
        /// Client sending the message. This value may be null if not sent from a client.
        /// </summary>
        public NetworkConnection Sender;
        /// <summary>
        /// True if the message was sent, false if received. This is applicable with direct messages.
        /// </summary>
        public bool Outbound;
        /// <summary>
        /// Message sent.
        /// </summary>
        public string Message;

        public ChatMessage(ushort messageType, NetworkConnection receiver, NetworkConnection sender, string message, bool outbound)
        {
            MessageType = messageType;
            Receiver = receiver;
            Sender = sender;
            Message = message;
            Outbound = outbound;
        }
    }


}