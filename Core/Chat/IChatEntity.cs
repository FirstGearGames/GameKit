using FishNet.Connection;
using OldFartGames.Gameplay.Canvases.Chats;

public interface IChatEntity
{
    public string GetEntityName();
    public NetworkConnection GetConnection();
    public TeamTypes GetTeamType();
    public TeamTypes GetTeamType(NetworkConnection otherConnection);
}