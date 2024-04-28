
using FishNet;
using FishNet.Managing;
using UnityEngine;

namespace GameKit.Core.Inventories.Bags
{
    /// <summary>
    /// Information about a bag.
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "New Bag", menuName = "Game/Inventory/BagData")]
    public class BagData : ScriptableObject
    {
        /// <summary>
        /// Unique Id for this bag. This is generally a database Id for the bag.
        /// </summary>
        [System.NonSerialized]
        public uint UniqueId = InventoryConsts.UNSET_BAG_ID;
        /// <summary>
        /// Maximum amount of slots in this bag.
        /// </summary>
        public int Space;
        /// <summary>
        /// Name of the bag.
        /// </summary>
        public string Name;
        /// <summary>
        /// Description of the bag.
        /// </summary>
        public string Description;
    }

    public static class BagDataExtensions
    {
        /// <summary>
        /// Makes this object network serializable.
        /// </summary>
        /// <returns></returns>
        public static SerializableBagData ToSerializable(this BagData bd) => new SerializableBagData(bd.UniqueId);

        /// <summary>
        /// Makes this object native.
        /// </summary>
        /// <returns></returns>
        /// <param name="bagManager">BagManager to use. If null InstanceFinder will be used.</param>
        public static BagData ToNative(this SerializableBagData sbd, BagManager bagManager = null)
        {
            if (bagManager == null)
            {
                if (!InstanceFinder.TryGetInstance<BagManager>(out bagManager))
                {
                    NetworkManagerExtensions.LogError($"BagManager could not be found.");
                    return default;
                }
            }

            return bagManager.GetBagData(sbd.UniqueId);
        }
    }
}