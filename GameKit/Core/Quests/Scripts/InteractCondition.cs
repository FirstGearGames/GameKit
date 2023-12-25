using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Interact Condition", menuName = "GameKit/Quests/Interact Condition")]
    public class InteractCondition : QuestConditionBase
    {
        /// <summary>
        /// Object to interact with.
        /// This could be an NPC, world object, and more.
        /// </summary>
        public uint ObjectId;
    }


}