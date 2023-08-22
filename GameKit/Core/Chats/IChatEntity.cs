using FishNet.Connection;

namespace GameKit.Chats
{

    public interface IChatEntity
    {
        public string GetEntityName();
        public NetworkConnection GetConnection();
        public TeamTypes GetTeamType();
        public TeamTypes GetTeamType(NetworkConnection otherConnection);
    }

}