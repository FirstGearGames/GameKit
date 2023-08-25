using UnityEngine;
using GameKit.Bundles.CraftingAndInventories.Resources;
using GameKit.Core.Resources;
using GameKit.Core.Crafting;
using GameKit.Bundles.CraftingAndInventories.Crafting;

namespace GameKit.Bundles.CraftingAndInventories.Managers
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
        private Recipe[] _recipes = new Recipe[0];
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
            rm.AddIResourceData((IResourceData[])_resourceDatas);
            rm.AddIResourceCategoryData((IResourceCategoryData[])_resourceCategoryDatas);

            CraftingManager cm = GetComponentInParent<CraftingManager>();
            cm.AddIRecipe(_recipes);
        }

    }
}