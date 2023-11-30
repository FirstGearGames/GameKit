using FishNet.Connection;

namespace GameKit.Core.Chats.Canvases
{
    /// <summary>
    /// Used to display chat and take chat input from the local client.
    /// </summary>
    public class ChatCanvas : ChatCanvasBase
    {
  
        /// <summary>
        /// Returns TeamType of a to b.
        /// </summary>
        protected override TeamType GetTeamType(NetworkConnection a, NetworkConnection b)
        {
            /* Implement this properly when you have a team
             * system setup for your game. */
            if (a == b)
                return TeamType.Self;
            else
                return TeamType.Friendly;
        }

    }

}