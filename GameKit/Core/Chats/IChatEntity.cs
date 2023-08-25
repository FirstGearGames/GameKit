using FishNet.Connection;
using GameKit.Dependencies.Utilities;

namespace GameKit.Core.Chats
{

    public interface IChatEntity : IResettable
    {
        public string GetEntityName();
        public NetworkConnection GetConnection();
        public ushort GetTeamType();
        public ushort GetTeamType(NetworkConnection otherConnection);
        public void Initialize(NetworkConnection connection, string entityName);
    }

}