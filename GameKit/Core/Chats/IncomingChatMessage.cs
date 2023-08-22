using FishNet.Connection;

namespace GameKit.Chats
{

    /// <summary>
    /// Sets weaponIds on server objects.
    /// </summary>
    public struct IncomingChatMessage
    {
        /// <summary>
        /// Type of message received.
        /// </summary>
        public MessageTargetTypes TargetType;
        /// <summary>
        /// Client receiving the message. This value may be null if not a direct message.
        /// </summary>
        public NetworkConnection Receiver;
        /// <summary>
        /// Client sending the message.
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

        public IncomingChatMessage(MessageTargetTypes targetType, NetworkConnection receiver, NetworkConnection sender, string message, bool outbound)
        {
            TargetType = targetType;
            Receiver = receiver;
            Sender = sender;
            Message = message;
            Outbound = outbound;
        }
    }


}