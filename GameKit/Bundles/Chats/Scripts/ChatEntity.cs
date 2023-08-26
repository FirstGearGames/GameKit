using FishNet.Connection;
using GameKit.Core.Chats;
using GameKit.Dependencies.Utilities;

namespace GameKit.Bundles.Chats
{

    public class ChatEntity : IChatEntity, IResettable
    {
        public NetworkConnection Connection { get; private set; }
        public string EntityName { get; private set; }
        public string GetEntityName() => EntityName;
        public NetworkConnection GetConnection() => Connection;

        public ChatEntity() { }
        public ChatEntity(NetworkConnection connection, string entityName)
        {
            Connection = connection;
            EntityName = entityName;
        }

        public void Initialize(NetworkConnection conn, string entityName)
        {
            Connection = conn;
            EntityName = entityName;
        }

        public void ResetState()
        {
            Connection = null;
            EntityName = string.Empty;
        }

        public void InitializeState() { }

        public void SetEntityName(string entityName)
        {
            EntityName = entityName;
        }

    }


}