using FishNet.Connection;
using OldFartGames.Gameplay.Canvases.Chats;

public class ChatEntity : IChatEntity
{

    public NetworkConnection Connection { get; private set; }
    public string EntityName {get;private set; }

    public string GetEntityName() => EntityName;
    public NetworkConnection GetConnection() => Connection;
    public TeamTypes GetTeamType() => TeamTypes.Friendly;
    public TeamTypes GetTeamType(NetworkConnection otherConnection) => TeamTypes.Friendly;

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

    public void Reset()
    {
        Connection = null;
        EntityName = string.Empty;
    }
}