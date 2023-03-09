using FishNet.Managing;
using UnityEngine;

using Inventories = GameKit.Configurations.Inventories;

namespace GameKit.Configurations.Managing
{

    public class ConfigurationManager : MonoBehaviour
    {
        [SerializeField]
        private NetworkManager _networkManager;

        [SerializeField]
        private Inventories.GraphicsConfiguration _inventoryGraphics;
        public Inventories.GraphicsConfiguration Inventory => _inventoryGraphics;

        private void Awake()
        {
            _networkManager.RegisterInstance<ConfigurationManager>(this);
        }


    }


}