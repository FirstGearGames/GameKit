using GameKit.Core.Inventories.Canvases;
using GameKit.Core.Providers;
using GameKit.Dependencies.Utilities;
using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Resources.Droppables
{

    /// <summary>
    /// A resource which can be dropped.
    /// </summary>
    [CreateAssetMenu(fileName = "Droppable", menuName = "Game/New Droppable", order = 1)]
    public class Droppable : ScriptableObject, IWeighted
    {
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
        //todo: make sure droppable quantity is a minimum of 1 in some manager.

    }

    public static class DroppableExtensions
    {
        /// <summary>
        /// Converts droppable results to a ResourceQuantity collection.
        /// </summary>
        public static void ToResourceQuantities(this Dictionary<Droppable, byte> drops, ref List<ResourceQuantity> results)
        {
            foreach (KeyValuePair<Droppable, byte> item in drops)
            {
                ResourceQuantity rq = new ResourceQuantity(item.Key.ResourceData.UniqueId, item.Value);
                results.Add(rq);
            }
        }
    }
}

