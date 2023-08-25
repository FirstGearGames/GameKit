using FishNet.Connection;
using GameKit.Core.Chats;

namespace GameKit.Bundles.Chats
{

    public class ChatEntity : IChatEntity
    {

        public NetworkConnection Connection { get; private set; }
        public string EntityName { get; private set; }

        public string GetEntityName() => EntityName;
        public NetworkConnection GetConnection() => Connection;
        public ushort GetTeamType() => (ushort)TeamType.Friendly;
        public ushort GetTeamType(NetworkConnection otherConnection)
        {
            if (otherConnection == Connection)
                return (ushort)TeamType.Self;
            else
                return (ushort)TeamType.Friendly;
        }

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
    }


}