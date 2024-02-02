using GameKit.Core.Providers;
using GameKit.Dependencies.Utilities.Types;
using UnityEngine;

namespace GameKit.Core.Resources.Droppables
{

    /// <summary>
    /// A resource which can be dropped.
    /// </summary>
    [CreateAssetMenu(fileName = "Droppable", menuName = "Game/New Droppable", order = 1)]
    public class Droppable : ScriptableObject
    {
        public ResourceData ResourceData;
        public ByteRange Quantity = new ByteRange(1, 1);
        public ProviderData ProviderData;
        [Range(0f, 1f)]
        public float DropRate;
    }

}