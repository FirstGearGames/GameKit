
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
        public int UniqueId = UNSET_UNIQUEID;
        public void SetUniqueId(int id) => UniqueId = id;
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

        /// <summary>
        /// Id for an unset uniqueId.
        /// </summary>
        public const int UNSET_UNIQUEID = 0;

        /// <summary>
        /// Makes this object network serializable.
        /// </summary>
        /// <returns></returns>
        public SerializableBag ToSerializable() => new SerializableBag(UniqueId);
    }

}