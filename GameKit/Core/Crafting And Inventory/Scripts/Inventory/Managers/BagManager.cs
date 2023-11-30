
using FishNet.Managing;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Inventories.Bags
{
    public class BagManager : MonoBehaviour
    {
        /// <summary>
        /// All bags for the game.
        /// </summary>
        [Tooltip("All bags for the game.")]
        [SerializeField]
        private List<Bag> _bags = new List<Bag>();

        /// <summary>
        /// NetworkManager on or a parent of this object.
        /// </summary>
        private NetworkManager _networkManager;

        private void Awake()
        {
            InitializeOnce();
        }

        /// <summary>
        /// Initializes this for use.
        /// </summary>
        private void InitializeOnce()
        {
            _networkManager = GetComponentInParent<NetworkManager>();
            _networkManager.RegisterInstance(this);

            for (int i = 0; i < _bags.Count; i++)
                _bags[i].SetUniqueId(i);
        }

        /// <summary>
        /// Gets a bag.
        /// </summary>
        public Bag GetBag(int uniqueId)
        {
            if (uniqueId < 0 || uniqueId >= _bags.Count)
            {
                _networkManager.LogError($"Bag UniqueId {uniqueId} is out of bounds. Bags count is {_bags.Count}.");
                return new Bag();
            }
            else
            {
                return _bags[uniqueId];
            }
        }
    }
}