
using UnityEngine;

namespace GameKit.Core.Inventories.Bags
{
    public struct SerializableBag
    {
        /// <summary>
        /// Unique Id for this bag. This is generally a database Id for the bag.
        /// </summary>
        public int UniqueId;

        public SerializableBag(int uniqueId)
        {
            UniqueId = uniqueId;
        }
    }

}