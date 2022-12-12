using FishNet.Object;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Crafting.Managers
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
        public List<IRecipe> Recipes = new List<IRecipe>();
        #endregion

        #region Private.
        /// <summary>
        /// Resources which can be made.
        /// Key: the resourceId to be made.
        /// Value: Recipe reference.
        /// </summary>
        private Dictionary<int, IRecipe> _recipesCached = new Dictionary<int, IRecipe>();
        #endregion

        public override void OnStartServer()
        {
            base.OnStartServer();
            base.NetworkManager.RegisterInstance(this);
        }

        /// <summary>
        /// Adds recipe to Recipes.
        /// </summary>
        public void AddIRecipe(IRecipe recipe)
        {
            recipe.SetIndex(Recipes.Count);
            Recipes.Add(recipe);
            _recipesCached[recipe.GetIndex()] = recipe;
        }
        /// <summary>
        /// Adds recipes to Recipes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddIRecipe(IEnumerable<IRecipe> recipes)
        {
            foreach (IRecipe ir in recipes)
                AddIRecipe(ir);
        }

        /// <summary>
        /// Gets a recipe for a resource type.
        /// </summary>
        public IRecipe GetRecipe(int rId)
        {
            IRecipe result;
            if (!_recipesCached.TryGetValue(rId, out result))
                Debug.LogError($"Recipe not found for {rId}.");

            return result;
        }

    }

}


