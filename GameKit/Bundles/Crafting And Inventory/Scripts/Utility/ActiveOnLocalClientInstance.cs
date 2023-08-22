using UnityEngine;

namespace GameKit.Bundles.Utilities
{

    /// <summary>
    /// Sets active state based on if local client gains or loses ClientInstance.
    /// </summary>
    [DefaultExecutionOrder(short.MinValue + 1)]
    public class ActiveOnLocalClientInstance : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.SetActive(false);
            ClientInstance.OnClientChangeInvoke(new ClientInstance.ClientChangeDel(ClientInstance_OnClientChange));
        }

        private void OnDestroy()
        {
            ClientInstance.OnClientChange -= ClientInstance_OnClientChange;
        }


        /// <summary>
        /// Called when OnStart or OnStopClient calls for any ClientInstance.
        /// </summary>
        /// <param name="instance">Instance invoking.</param>
        /// <param name="started">True is Start was called.</param>
        private void ClientInstance_OnClientChange(ClientInstance instance, bool started)
        {
            if (instance == null)
            {
                gameObject.SetActive(false);
                return;
            }
            if (!instance.IsOwner)
                return;

            gameObject.SetActive(started);
        }

    }


}