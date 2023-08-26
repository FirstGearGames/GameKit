using FishNet.Object;
using FishNet.Connection;
using GameKit.Core.Chats.Managers;
using GameKit.Core.Chats;
using GameKit.Dependencies.Utilities;

namespace GameKit.Bundles.Chats.Managers
{

    /// <summary>
    /// Sets weaponIds on server objects.
    /// </summary>
    public class ChatManager : ChatManagerBase
    {
        /// <summary>
        /// Retrieve an IChatEntity from the cache.
        /// </summary>
        /// <returns></returns>
        protected override IChatEntity RetrieveIChatEntity()
        {
            return ResettableObjectCaches<ChatEntity>.Retrieve();
        }
        /// <summary>
        /// Store an IChatEntity to the cache.
        /// </summary>
        protected override void StoreIChatEntity(IChatEntity entity)
        {
            ResettableObjectCaches<ChatEntity>.Store((ChatEntity)entity);
        }
        /// <summary>
        /// Returns the messageType for direct messages.
        /// </summary>
        protected override ushort GetDirectMessageType() => (ushort)MessageType.Tell;
        /// <summary>
        /// Returns the messageType for world messages.
        /// </summary>
        protected override ushort GetWorldMessageType() => (ushort)MessageType.All;
        /// <summary>
        /// Returns the messageType for team messages.
        /// </summary>
        protected override ushort GetTeamMessageType() => (ushort)MessageType.Team;
        /// <summary>
        /// Returns if Sender can send a message to Target for messageType.
        /// Target may be null if message type does not require the field, such as world messages.
        /// </summary>
        /// <param name="sender">Client sending the message.</param>
        /// <param name="target">Client to receive the message. Value may be null.</param>
        /// <param name="messageType">MessageType being sent.</param>
        /// <returns>True if the message gets sent.</returns>
        protected override bool CanSendMessage(NetworkConnection sender, NetworkConnection target, ushort messageType)
        {
            /* Returning true allows players to send messages for all
             * message types without any conditions. As your game gains mechanics
             * you will likely want to override this method. Example usages:
             * 
             * Return false if messageType is for Team and target is not on the same team
             * as sender.
             * 
             * Another possibility is return false if target has sender blocked. You can
             * also accomplish this on the incoming message events to filter them on the local
             * client rather than on the server. */
            return true;
            /* There are still internal checks
             * to prevent abuse of the chat system, such as sending to invalid clients
             * or too often. */

        }

    }


}