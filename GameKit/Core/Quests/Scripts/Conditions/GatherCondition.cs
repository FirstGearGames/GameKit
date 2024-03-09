using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Gather Condition", menuName = "Game/Quests/Gather Condition")]
    public class GatherCondition : QuestConditionBase
    {
        /// <summary>
        /// Type of condition which must be met.
        /// </summary>
        public override ConditionType QuestType => ConditionType.Gather;
        /// <summary>
        /// Resources which must be gathered.
        /// </summary>
        public List<GatherableResource> Resources;        
    }


}