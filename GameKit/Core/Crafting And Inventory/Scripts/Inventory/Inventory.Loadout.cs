using FishNet.Connection;
using FishNet.Object;
using GameKit.Core.Inventories.Bags;

namespace GameKit.Core.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {

        private void OnSpawnServer_Loadout(NetworkConnection c)
        {

        }

        

        /// <summary>
        /// Sends the players inventory loadout in the order they last used.
        /// </summary>
        [TargetRpc]
        private void TgtApplyLoadout(NetworkConnection c, ActiveBag[] bags)
        {

        }
  
    }

}