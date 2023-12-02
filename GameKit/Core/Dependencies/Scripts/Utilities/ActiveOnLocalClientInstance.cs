using UnityEngine;

namespace GameKit.Core.Dependencies.Utilities
{

    /// <summary>
    /// Sets active state based on if local client gains or loses ClientInstance.
    /// </summary>
    [DefaultExecutionOrder(short.MinValue + 1)]
    public class ActiveOnLocalClientInstance : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// True to deactivate on awake, false to deactivate on start.
        /// </summary>
        [Tooltip("True to deactivate on awake, false to deactivate on start.")]
        [SerializeField]
        private bool _deactiveOnAwake = true;
        #endregion

        private void Awake()
        {
            ClientInstance.OnClientInstanceChangeInvoke(new ClientInstance.ClientInstanceChangeDel(ClientInstance_OnClientInstanceChange), false);
            if (_deactiveOnAwake)
                Deactivate();
        }
        /// <summary>
        /// Disable in Start to other objects can initialize their Awake.
        /// </summary>
        private void Start()
        {
            if (!_deactiveOnAwake)
                Deactivate();            
        }

        private void OnDestroy()
        {
            ClientInstance.OnClientInstanceChange -= ClientInstance_OnClientInstanceChange;
        }

        /// <summary>
        /// Sets gameObject inactive.
        /// </summary>
        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when OnStart or OnStopClient calls for any ClientInstance.
        /// </summary>
        /// <param name="instance">Instance invoking.</param>
        /// <param name="state">State of the change.</param>
        private void ClientInstance_OnClientInstanceChange(ClientInstance instance, ClientInstanceState state, bool asServer)
        {
            if (asServer)
                return;
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