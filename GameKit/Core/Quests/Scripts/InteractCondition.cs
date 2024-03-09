using GameKit.Core.Providers;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Interact Condition", menuName = "Game/Quests/Interact Condition")]
    public class InteractCondition : QuestConditionBase
    {
        /// <summary>
        /// Type of condition which must be met.
        /// </summary>
        public override ConditionType QuestType => ConditionType.Interactive;
        /// <summary>
        /// Provider to interact with.
        /// </summary>
        public ProviderData ProviderData;
    }


}