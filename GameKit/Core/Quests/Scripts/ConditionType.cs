using UnityEngine;

namespace GameKit.Core.Quests
{

    public enum ConditionType
    {
        Unset = 0,
        /// <summary>
        /// Gather objects.
        /// </summary>
        Gather = 1,
        /// <summary>
        /// Travel to a location.
        /// </summary>
        Travel = 2,
        /// <summary>
        /// Interact with objects.
        /// </summary>
        Interactive = 3,
    }


}