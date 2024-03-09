using UnityEngine;

namespace GameKit.Core.Resources
{

    [CreateAssetMenu(fileName = "New ResourceData", menuName = "Game/Resources/ResourceData", order = 1)]
    public class ResourceData : ResourceDataBase
    {
        /// <summary>
        /// Type of categories this resource fits into.
        /// </summary>
        public ResourceCategory Category;
        /// <summary>
        /// Display name of the resource.
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// Description for the resource.
        /// </summary>
        [Multiline]
        public string Description;
        /// <summary>
        /// Icon for the resource.
        /// </summary>
        public Sprite Icon;
    }
}