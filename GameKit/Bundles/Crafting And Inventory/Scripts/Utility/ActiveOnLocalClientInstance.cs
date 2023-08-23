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
        /// <param name="state">State of the change.</param>
        private void ClientInstance_OnClientChange(ClientInstance instance, ClientInstanceState state)
        {
            if (instance == null)
            {
                gameObject.SetActive(false);
                return;
            }
            if (!instance.IsOwner)
                return;

            if (state.IsPreState())
                return;

            bool started = state.IsInitializeState();
            gameObject.SetActive(started);
        }

    }


}