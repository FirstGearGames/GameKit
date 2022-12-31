using FishNet;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Sets active state based on if local client is ready or not.
/// </summary>
public class ActiveOnLocalClient : MonoBehaviour
{

    private void Awake()
    {
        InstanceFinder.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
        InstanceFinder.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        //Force to deactivate if not client.
        if (!InstanceFinder.IsClient)
            ClientManager_OnClientConnectionState(new ClientConnectionStateArgs() { ConnectionState = LocalConnectionState.Stopped });
    }

    private void OnDestroy()
    {
        if (InstanceFinder.NetworkManager == null)
            return;

        InstanceFinder.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
        InstanceFinder.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
    }

    /// <summary>
    /// Called when the connection state changes for the local client.
    /// </summary>
    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
    {
        if (obj.ConnectionState != LocalConnectionState.Started)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when a client loads start scenes.
    /// </summary>
    /// <param name="arg1">Connection which loaded scenes.</param>
    /// <param name="asServer">True if callback is for the server.</param>
    private void SceneManager_OnClientLoadedStartScenes(FishNet.Connection.NetworkConnection arg1, bool asServer)
    {
        if (!asServer && arg1.IsLocalClient)
            gameObject.SetActive(true);
    }
}
