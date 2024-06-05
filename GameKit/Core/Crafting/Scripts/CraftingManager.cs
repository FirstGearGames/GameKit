using FishNet.Object;
using GameKit.Core.Resources;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Core.Crafting
{

    /// <summary>
    /// Holds information needed for crafting.
    /// </summary>
    public class CraftingManager : NetworkBehaviour
    {
        #region Serialized.
        /// <summary>
        /// All recipes for this game.
        /// </summary>
        [System.NonSerialized, HideInInspector]
        public List<RecipeData> RecipeDatas = new List<RecipeData>();
        #endregion

        #region Private.
        /// <summary>
        /// Resources which can be made.
        /// Key: the resourceId to be made.
        /// Value: Recipe reference.
        /// </summary>
        private Dictionary<uint, RecipeData> _recipeDatasCached = new Dictionary<uint, RecipeData>();
        #endregion

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            base.NetworkManager.RegisterInstance(this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            base.NetworkManager.UnregisterInstance<CraftingManager>();
        }

        /// <summary>
        /// Adds recipe to Recipes.
        /// </summary>
        public void AddRecipeData(RecipeData data, bool applyUniqueId)
        {
            if (!data.Enabled)
                return;
            if (applyUniqueId)
                data.UniqueId = (uint)(RecipeDatas.Count + ResourceConsts.UNSET_RESOURCE_ID + 1);

            RecipeDatas.Add(data);
            _recipeDatasCached[data.UniqueId] = data;
        }
        /// <summary>
        /// Adds recipes to Recipes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRecipeData(IEnumerable<RecipeData> datas, bool applyUniqueId)
        {
            foreach (RecipeData rd in datas)
                AddRecipeData(rd, applyUniqueId);
        }

        /// <summary>
        /// Gets a recipe for a resource type.
        /// </summary>
        public RecipeData GetRecipe(uint rId)
        {
            RecipeData result;
            if (!_recipeDatasCached.TryGetValue(rId, out result))
                Debug.LogError($"Recipe not found for {rId}.");

            return result;
        }

    }

}


