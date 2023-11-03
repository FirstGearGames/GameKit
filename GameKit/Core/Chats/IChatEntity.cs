using FishNet.Connection;

namespace GameKit.Core.Chats
{

    public interface IChatEntity
    {
        /// <summary>
        /// Name for this client.
        /// </summary>
        public string GetEntityName();
        /// <summary>
        /// Updates the name for this entity.
        /// </summary>
        /// <param name="entityName">New value.</param>
        public void SetEntityName(string entityName);
        /// <summary>
        /// Client for this entity.
        /// </summary>
        /// <returns></returns>
        public NetworkConnection GetConnection();
        /// <summary>
        /// Initializes this for use.
        /// </summary>
        /// <param name="connection">Client this entity is for.</param>
        /// <param name="entityName">Name of the client.</param>
        public void Initialize(NetworkConnection connection, string entityName);
    }

}