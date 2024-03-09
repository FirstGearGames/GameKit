using UnityEngine;
using GameKit.Core.Resources;
using GameKit.Core.Resources.Droppables;

namespace GameKit.Core.Crafting.Managers
{

    /// <summary>
    /// Loads datas into their managers.
    /// </summary>
    public class DatasLoader : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// All resource datas for this game.
        /// </summary>
        [Tooltip("All resource datas for this game.")]
        [SerializeField]
        private ResourceData[] _resourceDatas = new ResourceData[0];
        /// <summary>
        /// All resource category datas for this game.
        /// </summary>
        [Tooltip("All resource category datas for this game.")]
        [SerializeField]
        private ResourceCategoryData[] _resourceCategoryDatas = new ResourceCategoryData[0];
        /// <summary>
        /// All recipes for this game.
        /// </summary>
        [Tooltip("All recipes for this game.")]
        [SerializeField]
        private RecipeData[] _recipeDatas = new RecipeData[0];
        /// <summary>
        /// All droppables for this game.
        /// </summary>
        [Tooltip("All droppables for this game.")]
        [SerializeField]
        private DroppableData[] _droppableDatas = new DroppableData[0];
        #endregion

        private void Awake()
        {
            AddDatasToManagers();
        }

        /// <summary>
        /// Adds all datas to each appropriate manager.
        /// </summary>
        private void AddDatasToManagers()
        {
            ResourceManager rm = GetComponentInParent<ResourceManager>();
            rm.AddResourceData(_resourceDatas, true);
            rm.AddResourceCategoryData(_resourceCategoryDatas);

            CraftingManager cm = GetComponentInParent<CraftingManager>();
            cm.AddRecipeData(_recipeDatas, true);

            DroppableManager dm = GetComponentInParent<DroppableManager>();
            dm.AddDroppableData(_droppableDatas, true);
        }

    }
}