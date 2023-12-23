using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Interact Objective", menuName = "GameKit/Quests/Interact Objective")]
    public class InteractObjective : QuestObjectiveBase
    {
        /// <summary>
        /// Object to interact with.
        /// This could be an NPC, world object, and more.
        /// </summary>
        public uint ObjectId;
    }


}