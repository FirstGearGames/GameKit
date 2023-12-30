using GameKit.Core.Resources;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Travel Condition", menuName = "GameKit/Quests/Travel Condition")]
    public class TravelCondition : QuestConditionBase
    {
        /// <summary>
        /// Objects which must be acquired to indicate visiting a region.
        /// </summary>
        public List<ResourceData> Objects;
    }


}