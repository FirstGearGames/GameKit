using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Gather Condition", menuName = "GameKit/Quests/Gather Condition")]
    public class GatherCondition : QuestConditionBase
    {
        public List<GatherableResource> Resources;
    }


}