using GameKit.Core.Resources;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Travel Condition", menuName = "Game/Quests/Travel Condition")]
    public class TravelCondition : QuestConditionBase
    {
        /// <summary>
        /// Type of condition which must be met.
        /// </summary>
        public override ConditionType QuestType => ConditionType.Travel;
        /// <summary>
        /// Objects which must be acquired to indicate visiting a region.
        /// </summary>
        public List<ResourceData> Objects;
    }


}