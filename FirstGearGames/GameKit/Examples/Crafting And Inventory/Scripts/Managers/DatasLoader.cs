using GameKit.Crafting.Managers;
using GameKit.Examples.Crafting;
using GameKit.Examples.Resources;
using GameKit.Resources.Managers;
using TriInspector;
using UnityEngine;

namespace GameKit.Examples.Managers
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
        [PropertyTooltip("All resource datas for this game.")]
        [SerializeField]
        private ResourceData[] _resourceDatas = new ResourceData[0];
        /// <summary>
        /// All resource category datas for this game.
        /// </summary>
        [PropertyTooltip("All resource category datas for this game.")]
        [SerializeField]
        private ResourceCategoryData[] _resourceCategoryDatas = new ResourceCategoryData[0];
        /// <summary>
        /// All recipes for this game.
        /// </summary>
        [PropertyTooltip("All recipes for this game.")]
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
            rm.AddIResourceData(_resourceDatas);
            rm.AddIResourceCategoryData(_resourceCategoryDatas);

            CraftingManager cm = GetComponentInParent<CraftingManager>();
            cm.AddIRecipe(_recipes);
        }

    }
}