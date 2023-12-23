using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Gather Objective", menuName = "GameKit/Quests/Gather Objective")]
    public class GatherObjective : QuestObjectiveBase
    {
        public List<GatherableResource> Resources;
    }


}