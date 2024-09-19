using UnityEngine;

namespace GameKit.Core.Quests
{
    public abstract class QuestConditionBase : ScriptableObject
    {
        /// <summary>
        /// Type of condition which must be met.
        /// </summary>
        public abstract ConditionType QuestType { get; }
    }

}