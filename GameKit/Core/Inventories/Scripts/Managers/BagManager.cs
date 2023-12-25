
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
        /// <summary>
        /// Offset applied to a bag's UniqueId when setting or getting from _bags.
        /// </summary>
        public const int BAG_ID_OFFSET = 1;

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
                _bags[i].SetUniqueId(i + 1);
        }

        /// <summary>
        /// Gets a bag.
        /// </summary>
        public Bag GetBag(int uniqueId)
        {
            //UniqueIds for bags start on 1. A value of 0 is unset.
            if (uniqueId < 1 || uniqueId > _bags.Count)
            {
                _networkManager.LogError($"Bag UniqueId {uniqueId} is out of bounds. Id cannot be less than {BAG_ID_OFFSET} nor more than bags count of {_bags.Count}.");
                return new Bag();
            }
            else
            {
                return _bags[uniqueId - 1];
            }
        }
    }
}