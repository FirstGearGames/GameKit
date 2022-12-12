using FishNet.Object;
using GameKit.Examples.Inventories.Canvases;
using UnityEngine;

namespace GameKit.Examples.Managers
{

    /// <summary>
    /// Gameplay canvases register to this manager.
    /// </summary>
    public class CanvasManager :  NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// InventoryCanvas reference.
        /// </summary>
        [HideInInspector]
        public InventoryCanvas InventoryCanvas;
        #endregion

        public override void OnStartServer()
        {
            base.OnStartServer();
            base.NetworkManager.RegisterInstance(this);
        }

    }


}