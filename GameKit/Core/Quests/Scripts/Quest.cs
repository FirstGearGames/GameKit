using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Quest", menuName = "GameKit/Quests/Quest", order = int.MinValue)]
    public class Quest : ScriptableObject
    {
        /// <summary>
        /// UniqueId for this quest.
        /// </summary>
        public uint UniqueId;
        /// <summary>
        /// True if the quest can be seen in a tracker.
        /// </summary>
        public bool Trackable;
        /// <summary>
        /// Title for the quest.
        /// </summary>
        public string Title;
        /// <summary>
        /// Description of the quest.
        /// </summary>
        public string Description;
        /// <summary>
        /// Objective details for the quest.
        /// </summary>
        public List<QuestConditionBase> Conditions;
        /// <summary>
        /// Rewards for completing the quest.
        /// </summary>
        public List<QuestRewardBase> Rewards;
    }

}