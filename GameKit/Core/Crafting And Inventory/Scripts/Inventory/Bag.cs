
using UnityEngine;

namespace GameKit.Core.Inventories.Bags
{
    /// <summary>
    /// Information about a bag.
    /// </summary>
    [CreateAssetMenu(fileName = "New Bag", menuName = "GameKit/Inventory/Create Bag")]
    public class Bag : ScriptableObject
    {
        /// <summary>
        /// Unique Id for this bag. This is generally a database Id for the bag.
        /// </summary>
        [System.NonSerialized]
        public int UniqueId = -1;
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
    }

}