using FishNet;
using FishNet.Transporting;
using UnityEngine;

namespace GameKit.Examples.Utilities
{

    /// <summary>
    /// Sets active state based on if local client gains or loses ClientInstance.
    /// </summary>
    public class ActiveOnLocalClientInstance : MonoBehaviour
    {
        private void Awake()
        {
            ClientInstance.OnClientChange += ClientInstance_OnClientChange;
            /* Invoke a start immediately using the clients
             * current instance. If it's null then the object will
             * deactivate. */
            ClientInstance_OnClientChange(ClientInstance.Instance, true);
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