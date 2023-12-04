using FishNet.Connection;
using FishNet.Object;
using GameKit.Core.Inventories.Bags;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace GameKit.Core.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {

        private void OnStartServer_Loadout()
        {
            BagManager bm = base.NetworkManager.GetInstance<BagManager>();
            foreach (Bag item in _defaultBags)
            {
                Bag b = bm.GetBag(item.UniqueId);
                AddBag(b);
            }
        }
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

        private string InventoryToJson()
        {
            string result = JsonConvert.SerializeObject(Bags.ToSerializable());
            return result;
        }
        private void Update()
        {
            if (Time.frameCount % 300 == 0)
                SaveLoadout();
        }
        private void SaveLoadout()
        {
            //string path = Application.dataPath;
            string s = InventoryToJson();
            Debug.Log(s);

        }
  
    }

}