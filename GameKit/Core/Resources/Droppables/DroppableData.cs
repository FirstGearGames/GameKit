using GameKit.Dependencies.Utilities;
using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Resources.Droppables
{

    /// <summary>
    /// A resource which can be dropped.
    /// </summary>
    [CreateAssetMenu(fileName = "New Droppable", menuName = "Game/Resources/DroppableData", order = 1)]
    public class DroppableData : ScriptableObject, IWeighted
    {
        /// <summary>
        /// True if should be recognized and used. False to remove from the game.
        /// </summary>
        public bool Enabled = true;
        /// <summary>
        /// UniqueId of the droppable.   
        /// </summary>
        [HideInInspector, System.NonSerialized]
        public uint UniqueId = ResourceConsts.UNSET_RESOURCE_ID;
        /// <summary>
        /// Resource to drop.
        /// </summary>
        public ResourceData ResourceData;
        /// <summary>
        /// Number of resources which can drop per successful drop.
        /// </summary>
        public ByteRange Quantity = new ByteRange(1, 1);
        /// <summary>
        /// Likeliness of this drop to occur.
        /// </summary>
        [Range(0f, 1f)]
        public float DropRate;

        public float GetWeight() => DropRate;
        public ByteRange GetQuantity() => Quantity;

    }

    public static class DroppableExtensions
    {
        /// <summary>
        /// Converts droppable results to a ResourceQuantity collection.
        /// </summary>
        public static void ToResourceQuantities(this Dictionary<DroppableData, byte> drops, ref List<ResourceQuantity> results)
        {
            foreach (KeyValuePair<DroppableData, byte> item in drops)
            {
                ResourceQuantity rq = new ResourceQuantity(item.Key.ResourceData.UniqueId, item.Value);
                results.Add(rq);
            }
        }
    }
}

