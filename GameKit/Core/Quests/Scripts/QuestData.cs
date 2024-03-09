using GameKit.Core.Providers;
using GameKit.Core.Resources.Droppables;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New QuestData", menuName = "Game/Quests/QuestData", order = int.MinValue)]
    public class QuestData : ScriptableObject
    {
        [System.Serializable]
        public struct QuestDroppableData
        {
            /// <summary>
            /// Provider which can drop the resource.
            /// </summary>
            public ProviderData Provider;
            /// <summary>
            /// Resource which can be dropped.
            /// </summary>
            public DroppableData Droppable;
        }

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
        /// <summary>
        /// Droppables which become available when this quest is active.
        /// </summary>
        public List<QuestDroppableData> QuestDroppables;
    }

}