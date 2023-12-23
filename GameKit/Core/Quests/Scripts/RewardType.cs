using UnityEngine;

namespace GameKit.Core.Quests
{

    public enum RewardType
    {
        Unset = 0,
        /// <summary>
        /// Gives resources.
        /// </summary>
        Resource = 1,
        /// <summary>
        /// Provides experience.
        /// </summary>
        Experience = 2,
        /// <summary>
        /// Unlocks another quest.
        /// </summary>
        Quest = 3
    }


}