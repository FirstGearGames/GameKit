using UnityEngine;

namespace GameKit.Core.Worlds
{
    //TODO make an area manager that has all these references and sets their uniqueIds

    /// <summary>
    /// Any area in any world which can be traveled to.
    /// </summary>
    [CreateAssetMenu(fileName = "New Area", menuName = "GameKit/Worlds/Area")]
    public class Area : ScriptableObject
    {
        /// <summary>
        /// Id for the area.
        /// </summary>
        public uint UniqueId;
    }


}