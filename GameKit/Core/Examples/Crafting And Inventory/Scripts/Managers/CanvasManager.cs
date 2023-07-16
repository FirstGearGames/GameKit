using FishNet.Object;
using GameKit.Examples.Inventories.Canvases;
using GameKit.Examples.Tooltips.Canvases;
using UnityEngine;

namespace GameKit.Examples.Managers
{

    /// <summary>
    /// Gameplay canvases register to this manager.
    /// </summary>
    public class CanvasManager : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// InventoryCanvas reference.
        /// </summary>
        [HideInInspector, System.NonSerialized]
        public InventoryCanvas InventoryCanvas;
        /// <summary>
        /// TooltipCanvas reference.
        /// </summary>
        [HideInInspector, System.NonSerialized]
        public TooltipCanvas TooltipCanvas;
        #endregion

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            base.NetworkManager.RegisterInstance(this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            base.NetworkManager.UnregisterInstance<CanvasManager>();
        }

    }


}