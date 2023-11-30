using FishNet.Connection;

namespace GameKit.Core.Chats
{

    /// <summary>
    /// Information about a blocked message.
    /// </summary>
    public struct BlockedChatMessage
    {
        /// <summary>
        /// Type of message received.
        /// </summary>
        public ushort MessageType;
        /// <summary>
        /// Sender of the message.
        /// </summary>
        public NetworkConnection Sender;
        /// <summary>
        /// Message sent.
        /// </summary>
        public string Message;
        /// <summary>
        /// Reason chat message was blocked.
        /// </summary>
        public BlockedChatReason Reason;

        public BlockedChatMessage(ushort messageType, NetworkConnection sender, string message, BlockedChatReason reason)
        {
            MessageType = messageType;
            Sender = sender;
            Message = message;
            Reason = reason;
        }
    }

    public enum BlockedChatReason : byte
    {
        /// <summary>
        /// Default value.
        /// </summary>
        Unset = 0,
        /// <summary>
        /// No response is needed. This only occurs when client is suspected of abusing the chat system.
        /// </summary>
        NoResponse = 1,
        /// <summary>
        /// Sent to an invalid userId.
        /// </summary>
        InvalidTargetId = 2,
        /// <summary>
        /// Invalid connection state as server or client.
        /// </summary>
        InvalidState = 3,
        /// <summary>
        /// User is sending too many messages too quickly. This is only used by the client.
        /// </summary>
        TooManyMessages = 4,
    }


}